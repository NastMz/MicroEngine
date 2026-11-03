# me_box2d: Box2D v3 (the source tree build.sh fetches at the pinned commit, box2d.env) plus the flat C layer
# (me_box2d.c), and me_box2d_selftest, which exercises the layer on the build machine (or under Node for wasm).
#   Desktop and Android: one shared library, me_box2d, with Box2D linked in statically and only the layer exported.
#   Emscripten and iOS: two static archives, me_box2d and box2d (browser builds link them with NativeFileReference).
# Determinism (box2d.org/posts/2024/08/determinism): no contraction of a*b+c into FMA for gcc and clang, the rule
# Box2D's own CMakeLists applies to itself; MSVC does not contract by default. One thread: no task system is set.
cmake_minimum_required(VERSION 3.22)
project(me_box2d C)

set(BOX2D_SOURCE "" CACHE PATH "Box2D v3 source tree at the pinned commit")
if(NOT EXISTS "${BOX2D_SOURCE}/CMakeLists.txt")
	message(FATAL_ERROR "BOX2D_SOURCE must point at a Box2D v3 source tree (build.sh fetches it)")
endif()

set(CMAKE_C_STANDARD 17)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(CMAKE_C_VISIBILITY_PRESET hidden)
# No dependency on the Visual C++ runtime being installed: the CRT is linked in.
set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded")
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(BOX2D_VALIDATE OFF CACHE BOOL "" FORCE)
set(BOX2D_PROFILE OFF CACHE BOOL "" FORCE)
add_subdirectory("${BOX2D_SOURCE}" box2d EXCLUDE_FROM_ALL)

if(EMSCRIPTEN OR IOS)
	set(ME_STATIC ON)
	add_library(me_box2d STATIC me_box2d.c)
	target_compile_definitions(me_box2d PUBLIC ME_BOX2D_STATIC)
else()
	set(ME_STATIC OFF)
	add_library(me_box2d SHARED me_box2d.c)
endif()
target_link_libraries(me_box2d PRIVATE box2d)
target_include_directories(me_box2d PUBLIC "${CMAKE_CURRENT_SOURCE_DIR}")

add_executable(me_box2d_selftest me_box2d_selftest.c)
target_link_libraries(me_box2d_selftest PRIVATE me_box2d)
if(ME_STATIC)
	target_link_libraries(me_box2d_selftest PRIVATE box2d)
endif()

foreach(target me_box2d me_box2d_selftest)
	if(CMAKE_C_COMPILER_ID MATCHES "GNU|Clang" AND NOT CMAKE_C_COMPILER_FRONTEND_VARIANT STREQUAL "MSVC")
		target_compile_options(${target} PRIVATE -ffp-contract=off
			"-ffile-prefix-map=${BOX2D_SOURCE}=box2d" "-ffile-prefix-map=${CMAKE_CURRENT_SOURCE_DIR}=me_box2d")
	endif()
endforeach()
# Box2D's own sources: the same prefix map, so no build-machine path ends up in the library.
if(CMAKE_C_COMPILER_ID MATCHES "GNU|Clang" AND NOT CMAKE_C_COMPILER_FRONTEND_VARIANT STREQUAL "MSVC")
	target_compile_options(box2d PRIVATE "-ffile-prefix-map=${BOX2D_SOURCE}=box2d")
endif()

if(MSVC)
	# Reproducible: no time stamp in the objects or the DLL, so the same inputs give the same checksum.
	target_compile_options(box2d PRIVATE /Brepro)
	target_compile_options(me_box2d PRIVATE /Brepro)
	target_link_options(me_box2d PRIVATE /Brepro)
endif()
if(UNIX AND NOT APPLE AND NOT EMSCRIPTEN AND NOT ME_STATIC)
	# Linux and Android: strip symbols the layer does not export, and keep the library free of build paths.
	target_link_options(me_box2d PRIVATE -s -Wl,--exclude-libs,ALL)
	target_link_libraries(me_box2d PRIVATE m)
endif()
if(APPLE AND NOT IOS)
	set_target_properties(me_box2d PROPERTIES MACOSX_RPATH ON INSTALL_NAME_DIR "@rpath")
endif()
if(EMSCRIPTEN)
	# The self-test runs under Node: one JavaScript file with the wasm embedded, exiting with main's status.
	target_link_options(me_box2d_selftest PRIVATE -sSINGLE_FILE=1 -sEXIT_RUNTIME=1 -sENVIRONMENT=node)
endif()
