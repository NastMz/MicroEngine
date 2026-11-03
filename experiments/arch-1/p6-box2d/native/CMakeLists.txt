# ARCH-1 P6: Box2D v3 (from BOX2D_SOURCE, consumed as a subproject, so its own determinism flags apply) plus the flat
# me_box2d layer. Desktop and Android: one shared library me_box2d (Box2D linked in statically). Emscripten: two
# static archives (libme_box2d.a, libbox2d.a) for NativeFileReference.
cmake_minimum_required(VERSION 3.22)
project(me_box2d C)

set(BOX2D_SOURCE "" CACHE PATH "Box2D v3 source tree")
option(ME_ORIGIN_API "Box2D >= v3.2.0 large-world API (queries take an origin)" ON)
if(NOT EXISTS "${BOX2D_SOURCE}/CMakeLists.txt")
	message(FATAL_ERROR "BOX2D_SOURCE must point at a Box2D v3 source tree")
endif()

set(CMAKE_C_STANDARD 17)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
add_subdirectory("${BOX2D_SOURCE}" box2d EXCLUDE_FROM_ALL)

if(EMSCRIPTEN)
	add_library(me_box2d STATIC me_box2d.c)
	target_link_libraries(me_box2d PRIVATE box2d)
else()
	# Box2D's own functions are exported from the shared library too (box2d_EXPORTS selects dllexport / default
	# visibility in box2d/base.h; the whole archive is linked), so the raw-binding probe can call them directly.
	add_library(me_box2d SHARED me_box2d.c)
	target_compile_definitions(box2d PRIVATE box2d_EXPORTS)
	target_link_libraries(me_box2d PRIVATE "$<LINK_LIBRARY:WHOLE_ARCHIVE,box2d>")
endif()
if(ME_ORIGIN_API)
	target_compile_definitions(me_box2d PRIVATE ME_ORIGIN_API)
endif()
# The same rule Box2D applies to itself: no contraction of a*b+c into FMA.
if(CMAKE_C_COMPILER_ID MATCHES "GNU|Clang" AND NOT CMAKE_C_COMPILER_FRONTEND_VARIANT STREQUAL "MSVC")
	target_compile_options(me_box2d PRIVATE -ffp-contract=off)
endif()
