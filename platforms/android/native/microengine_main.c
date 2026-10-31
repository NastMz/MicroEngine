// raylib's android_main (rcore_android.c) runs the program's main() on the native-activity thread. MicroEngine's
// game loop is .NET code, so this main() calls back into the activity: GameActivity.RunGame, a Java method the .NET
// host exports. raylib's own functions are then called from .NET on this same thread, as InitWindow requires.
#include <jni.h>
#include <android/log.h>
#include <android_native_app_glue.h>

struct android_app *GetAndroidApp(void); // defined by rcore_android.c

#define TAG "MicroEngine"

int main(int argc, char *argv[])
{
    (void)argc;
    (void)argv;
    struct android_app *app = GetAndroidApp();
    JavaVM *vm = app->activity->vm;
    JNIEnv *env = NULL;
    if ((*vm)->AttachCurrentThread(vm, &env, NULL) != JNI_OK)
    {
        __android_log_print(ANDROID_LOG_ERROR, TAG, "host: cannot attach the native-activity thread to the JVM");
        return 1;
    }
    int status = 0;
    jclass activity = (*env)->GetObjectClass(env, app->activity->clazz);
    jmethodID run = (*env)->GetMethodID(env, activity, "runGame", "()V");
    if (run == NULL)
    {
        (*env)->ExceptionClear(env);
        __android_log_print(ANDROID_LOG_ERROR, TAG, "host: the activity has no runGame() method");
        status = 1;
    }
    else
    {
        (*env)->CallVoidMethod(env, app->activity->clazz, run);
        if ((*env)->ExceptionCheck(env))
        {
            (*env)->ExceptionDescribe(env);
            (*env)->ExceptionClear(env);
            status = 1;
        }
    }
    (*env)->DeleteLocalRef(env, activity);
    (*vm)->DetachCurrentThread(vm);
    return status;
}
