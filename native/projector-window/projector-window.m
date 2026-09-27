/*
 * Shapes the window of Adobe's Flash projector for macOS, from inside its process (see ADR 0010).
 *
 * Loaded with DYLD_INSERT_LIBRARIES, which the projector's entitlements allow, to replace the size the projector asks
 * for its window, and to quit the projector when the player closes its window. The launcher configures it through the environment:
 *
 *   PROJECTOR_WINDOW_HEIGHT      the game's height, in points; its width keeps the stage's proportions
 *   PROJECTOR_WINDOW_FULLSCREEN  set to make the window fullscreen
 *
 * The projector is Intel only, so this library is too: it runs under Rosetta 2.
 *
 * Build: clang -arch x86_64 -dynamiclib -fobjc-arc -mmacosx-version-min=11.0 -framework AppKit
 *        -o libprojector-window.dylib projector-window.m
 */
#import <AppKit/AppKit.h>
#import <objc/runtime.h>
#include <stdlib.h>

/* The projector's own window, apart from the menu bar's and AppKit's. */
static NSString *const ProjectorWindowClass = @"FP_FPWindow";

static CGFloat game_height;
static BOOL fullscreen_asked;

/* The projector resizes its window to the stage once the movie is loaded. */
static NSRect shaped(NSWindow *window, NSRect frame)
{
    NSRect content = [window contentRectForFrameRect:frame];
    if (content.size.height <= 0)
        return frame;
    content.size.width = round(content.size.width * game_height / content.size.height);
    content.size.height = game_height;
    NSRect wanted = [window frameRectForContentRect:content];
    /* Keeps the window's top where the projector put it */
    wanted.origin.y = NSMaxY(frame) - wanted.size.height;
    return wanted;
}

static void shape_projector_window(void)
{
    Method method = class_getInstanceMethod([NSWindow class], @selector(setFrame:display:));
    void (*set_frame)(id, SEL, NSRect, BOOL) = (void (*)(id, SEL, NSRect, BOOL))method_getImplementation(method);
    BOOL fullscreen = getenv("PROJECTOR_WINDOW_FULLSCREEN") != NULL;
    method_setImplementation(method, imp_implementationWithBlock(^(NSWindow *window, NSRect frame, BOOL display) {
        /* ponytail: AppKit's zoom resizes through here too, and gets the game's height; tell them apart if players miss it */
        BOOL projector_resize = [NSStringFromClass([window class]) isEqualToString:ProjectorWindowClass] && !window.inLiveResize
            && !(window.styleMask & NSWindowStyleMaskFullScreen);
        if (projector_resize && game_height > 0)
            frame = shaped(window, frame);
        set_frame(window, @selector(setFrame:display:), frame, display);
        if (projector_resize && fullscreen && !fullscreen_asked) {
            fullscreen_asked = YES;
            dispatch_async(dispatch_get_main_queue(), ^{
                window.collectionBehavior |= NSWindowCollectionBehaviorFullScreenPrimary;
                [window toggleFullScreen:nil];
            });
        }
    }));
}

/* Like a Mac app, the projector keeps running once its window is closed: the launcher would wait for it forever. */
static void quit_when_projector_window_closes(void)
{
    [NSNotificationCenter.defaultCenter addObserverForName:NSWindowWillCloseNotification object:nil queue:nil
        usingBlock:^(NSNotification *closing) {
            if ([NSStringFromClass([closing.object class]) isEqualToString:ProjectorWindowClass])
                [NSApp terminate:nil];
        }];
}

/* Not atoi, like on Linux: returns 0 if not a number. */
static int positive_number(const char *text)
{
    int number = 0;
    for (; *text; text++) {
        if (*text < '0' || *text > '9' || number > 100000)
            return 0;
        number = number * 10 + (*text - '0');
    }
    return number;
}

__attribute__((constructor)) static void start(void)
{
    /* The projector's child processes don't need this library. */
    unsetenv("DYLD_INSERT_LIBRARIES");
    const char *height = getenv("PROJECTOR_WINDOW_HEIGHT");
    game_height = height ? positive_number(height) : 0;
    shape_projector_window();
    quit_when_projector_window_closes();
}
