/*
 * Shapes the window of Adobe's Flash projector for Linux (GTK 2), from inside its process (see ADR 0008).
 *
 * Loaded both with LD_PRELOAD, to replace the size the projector asks for its window, and as a GTK module
 * (GTK_MODULES), to hide its menu and URL bars. The launcher configures it through the environment:
 *
 *   PROJECTOR_WINDOW_HEIGHT      the game's height, in pixels; its width keeps the stage's proportions
 *   PROJECTOR_WINDOW_FULLSCREEN  set to make the window fullscreen
 *
 * GTK 2's headers aren't needed to build it: the few functions it calls are declared below.
 *
 * It needs glibc 2.34, like the bundled GTK 2 and NSS.
 *
 * Build: cc -shared -fPIC -O2 -o libprojector-window.so projector-window.c -ldl
 */
#define _GNU_SOURCE
#include <dlfcn.h>
#include <stdlib.h>

typedef unsigned long GType;
typedef struct { GType type; unsigned long data[2]; } GValue;
typedef struct GdkWindow GdkWindow;
typedef struct GtkWidget GtkWidget;

extern GType gtk_widget_get_type(void);
extern GType gtk_menu_bar_get_type(void);
extern GType gtk_entry_get_type(void);
extern GtkWidget *gtk_widget_get_parent(GtkWidget *widget);
extern void gtk_widget_hide(GtkWidget *widget);
extern GdkWindow *gdk_window_get_parent(GdkWindow *window);
extern GdkWindow *gdk_get_default_root_window(void);
extern void gdk_window_fullscreen(GdkWindow *window);
extern int g_type_check_instance_is_a(void *instance, GType type);
extern void *g_type_class_ref(GType type);
extern unsigned g_signal_lookup(const char *name, GType type);
extern unsigned long g_signal_add_emission_hook(unsigned signal, unsigned detail, void *hook, void *data, void *destroy);
extern void *g_value_get_object(const GValue *value);

/* The projector looks for a running browser with ps and grep: they don't need this library. */
__attribute__((constructor)) static void keep_out_of_child_processes(void)
{
    unsetenv("LD_PRELOAD");
}

/* Not atoi: glibc 2.38's headers turn it into __isoc23_strtol, which older systems lack. Returns 0 if not a number. */
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

static int is_top_level(GdkWindow *window)
{
    return gdk_window_get_parent(window) == gdk_get_default_root_window();
}

/* The projector resizes its window to the stage once the movie is loaded. */
void gdk_window_resize(GdkWindow *window, int width, int height)
{
    static void (*resize)(GdkWindow *, int, int);
    if (!resize)
        resize = (void (*)(GdkWindow *, int, int))dlsym(RTLD_NEXT, "gdk_window_resize");

    const char *game_height = getenv("PROJECTOR_WINDOW_HEIGHT");
    int wanted_height = game_height ? positive_number(game_height) : 0;
    if (wanted_height > 0 && is_top_level(window) && height > 0) {
        width = (int)(((long)width * wanted_height + height / 2) / height);
        height = wanted_height;
    }
    resize(window, width, height);

    if (getenv("PROJECTOR_WINDOW_FULLSCREEN") && is_top_level(window))
        gdk_window_fullscreen(window);
}

static int hide_menu_and_url_bars(void *hint, unsigned parameter_count, const GValue *parameters, void *data)
{
    (void)hint;
    (void)parameter_count;
    (void)data;
    GtkWidget *widget = g_value_get_object(&parameters[0]);
    if (g_type_check_instance_is_a(widget, gtk_menu_bar_get_type()))
        gtk_widget_hide(widget);
    else if (g_type_check_instance_is_a(widget, gtk_entry_get_type()))
        /* The URL entry and its "URL:" label */
        gtk_widget_hide(gtk_widget_get_parent(widget));
    return 1;
}

void gtk_module_init(int *argc, char ***argv)
{
    (void)argc;
    (void)argv;
    GType widget = gtk_widget_get_type();
    g_type_class_ref(widget);
    g_signal_add_emission_hook(g_signal_lookup("map", widget), 0, hide_menu_and_url_bars, NULL, NULL);
}
