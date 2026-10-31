// Why raylib cannot open a window on a Mac without a GPU (docs/evaluations/platform-2.md, "macOS"). Prints, as Markdown:
//   - the Metal device, if any;
//   - every OpenGL renderer CGL reports (accelerated or not);
//   - whether a pixel format exists for GLFW's request (raylib 6.0 opens an OpenGL 3.3 core context; GLFW asks
//     NSOpenGL for 3.2+ core with NSOpenGLPFAAccelerated, always: external/glfw/src/nsgl_context.m), then the same
//     request without the accelerated requirement, and the renderer a context created from it reports.
// Exit status 0 whatever it finds; the summary is the result. Build: clang -fobjc-arc gl-probe.m -framework OpenGL
// -framework Metal -framework Foundation -o gl-probe (OpenGL is deprecated on macOS; the warnings are expected).
#define GL_SILENCE_DEPRECATION
#import <Foundation/Foundation.h>
#import <Metal/Metal.h>
#import <OpenGL/OpenGL.h>
#import <OpenGL/gl3.h>

static void tryFormat(const char *label, BOOL accelerated)
{
    CGLPixelFormatAttribute attributes[16];
    int n = 0;
    if (accelerated) attributes[n++] = kCGLPFAAccelerated;
    attributes[n++] = kCGLPFAClosestPolicy;
    attributes[n++] = kCGLPFAOpenGLProfile; attributes[n++] = (CGLPixelFormatAttribute)kCGLOGLPVersion_3_2_Core;
    attributes[n++] = kCGLPFAColorSize; attributes[n++] = (CGLPixelFormatAttribute)24;
    attributes[n++] = kCGLPFAAlphaSize; attributes[n++] = (CGLPixelFormatAttribute)8;
    attributes[n++] = kCGLPFADepthSize; attributes[n++] = (CGLPixelFormatAttribute)24;
    attributes[n++] = kCGLPFADoubleBuffer;
    attributes[n] = (CGLPixelFormatAttribute)0;
    CGLPixelFormatObj format = NULL;
    GLint count = 0;
    CGLError error = CGLChoosePixelFormat(attributes, &format, &count);
    if (error != kCGLNoError || format == NULL)
    {
        printf("| %s | no pixel format (%s) | |\n", label, CGLErrorString(error));
        return;
    }
    CGLContextObj context = NULL;
    error = CGLCreateContext(format, NULL, &context);
    if (error != kCGLNoError)
    {
        printf("| %s | %d format(s), no context (%s) | |\n", label, count, CGLErrorString(error));
    }
    else
    {
        CGLSetCurrentContext(context);
        printf("| %s | %d format(s), context created | %s, OpenGL %s |\n", label, count,
               (const char *)glGetString(GL_RENDERER), (const char *)glGetString(GL_VERSION));
        CGLSetCurrentContext(NULL);
        CGLDestroyContext(context);
    }
    CGLDestroyPixelFormat(format);
}

int main(void)
{
    @autoreleasepool
    {
        NSProcessInfo *info = [NSProcessInfo processInfo];
        printf("### macOS OpenGL probe\n\n%s, %lu cores\n\n", [[info operatingSystemVersionString] UTF8String], (unsigned long)[info processorCount]);
        id<MTLDevice> metal = MTLCreateSystemDefaultDevice();
        printf("Metal device: %s\n\n", metal ? [[metal name] UTF8String] : "none");

        printf("| OpenGL renderer id | Accelerated | Online |\n| --- | --- | --- |\n");
        CGLRendererInfoObj renderers = NULL;
        GLint count = 0;
        if (CGLQueryRendererInfo(0xffffffff, &renderers, &count) == kCGLNoError)
        {
            for (GLint i = 0; i < count; i++)
            {
                GLint id = 0, accelerated = 0, online = 0;
                CGLDescribeRenderer(renderers, i, kCGLRPRendererID, &id);
                CGLDescribeRenderer(renderers, i, kCGLRPAccelerated, &accelerated);
                CGLDescribeRenderer(renderers, i, kCGLRPOnline, &online);
                printf("| 0x%08x | %s | %s |\n", id, accelerated ? "yes" : "no", online ? "yes" : "no");
            }
            CGLDestroyRendererInfo(renderers);
        }
        if (count == 0) printf("| none | | |\n");

        printf("\n| Request (3.2 core, RGBA8, depth 24, double buffer) | Result | Renderer |\n| --- | --- | --- |\n");
        tryFormat("GLFW's: accelerated required", YES);
        tryFormat("accelerated not required", NO);
        printf("\n");
    }
    return 0;
}
