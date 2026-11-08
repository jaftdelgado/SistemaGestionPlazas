import { Toast } from "@base-ui/react/toast";
import {
  Alert01Icon,
  Cancel01Icon,
  CancelCircleIcon,
  CheckmarkCircle01Icon,
  InformationCircleIcon,
} from "@hugeicons/core-free-icons";
import { HugeiconsIcon, type IconSvgElement } from "@hugeicons/react";
import {
  DEFAULT_TOAST_TIMEOUT,
  toastManager,
  type NotificationType,
} from "@/shared/lib/notifications";

/* Adapted from Bencho's "Toast stack" (MIT, bencho.dev/licence), rebuilt on
   Base UI's Toast: it owns the queue, the auto-dismiss timers, pausing them
   on hover or focus, swipe to dismiss and the screen-reader announcements.
   The look and the motion are Bencho's, in the classes below.

   ══ Toast stack ══════════════════════════════════════════
   Three toasts, always: the newest in front, the two before it
   tucked behind with a sliver of each showing above it, a step
   smaller each and a barely-there grey (the reference's tones). Point at the stack and it
   fans out into a list; move away and it gathers back up.

   ── ONE EASE, THREE PLACES ──────────────────────────────
   A toast's place is worked out from how far back it is (Base UI's
   --toast-index) and whether the stack is open, and the transition
   carries it there. Opening, closing, a toast leaving and the next
   arriving are all the same move — every toast heading for its new
   place on the same curve. Bencho springs it a touch under critical
   (stiffness 380, damping 34), which overshoots by well under 1%; the
   curve in the CSS is the same settle without the library.

   ── HOVER IS THE WHOLE STACK ────────────────────────────
   Base UI opens the stack when the pointer or focus is on the viewport,
   and a hairline under each toast bridges the gap between them, so the
   list never closes while the pointer crosses from one toast to the
   next. The front toast stays put as the rest fan downward.

   ── ONLY THREE ──────────────────────────────────────────
   Dismiss one and it slides away. The provider's limit is three: older
   ones are flagged limited and fade out rather than pile up. */

// Las clases se arman por partes para poder comentarlas; no pasan por cn() para que tailwind-merge no descarte ninguna.
function classes(...parts: string[]) {
  return parts.join(" ");
}

/* Tones are the reference's: the front toast white, the tucked ones a
   barely-there 2% grey — both the same, so they read as paper behind paper
   rather than a darkening ramp. Where the reference separates them with a
   soft shadow, this uses a faint hairline (no drop shadows on the bench).
   Status icons are their own solid colours, never faded. --tst-r is the
   corner radius; --tst-mute and --tst-wash are the muted text and the
   hover wash, both derived from the overlay tokens. Hovering or focusing
   the viewport (group/tst) also pauses the progress bar. */
const VIEWPORT = classes(
  "group/tst fixed top-4 left-1/2 z-60 w-[380px] font-sans text-overlay-foreground",
  "[translate:-50%_0]",
  "[--tst-r:24px]",
  "[--tst-mute:color-mix(in_srgb,var(--overlay-foreground)_60%,var(--overlay))]",
  "[--tst-wash:color-mix(in_srgb,var(--overlay-foreground)_12%,var(--overlay))]",
);

/* Base UI gives each toast its place in line (--toast-index, 0 at the
   front) and, once the stack is open, how far up it sits
   (--toast-offset-y). Everything else is worked out from those two.
   Behind the third, toasts stay put: they are limited and fade out. */
const TOAST = classes(
  "group/toast absolute top-0 right-0 left-0 box-border h-16 rounded-(--tst-r)",
  "[--peek:14px] [--gap:10px] [--back:min(var(--toast-index),2)]",
  "z-[calc(1000_-_var(--toast-index))]",
  // the stack: front at the top, each one behind a peek lower and a step smaller, hanging from its bottom edge
  "origin-[50%_100%]",
  "[transform:translateX(var(--toast-swipe-movement-x,0px))_translateY(calc(var(--toast-swipe-movement-y,0px)_+_var(--back)_*_var(--peek)))_scale(calc(1_-_var(--back)_*_0.05))]",
  // tucked: the same near-white for both, and a faint edge so each sliver has a line to read by; the front one has neither
  "[background:color-mix(in_srgb,var(--overlay-foreground)_calc(min(var(--toast-index),1)*8%),var(--overlay))]",
  "[box-shadow:0_0_0_1px_color-mix(in_srgb,var(--overlay-foreground)_calc(min(var(--toast-index),1)*14%),transparent)]",
  // the same settle for every move, a touch under critical
  "[transition:transform_320ms_cubic-bezier(0.22,1,0.36,1),translate_200ms_cubic-bezier(0.23,1,0.32,1),opacity_200ms_cubic-bezier(0.23,1,0.32,1),background-color_220ms_ease,box-shadow_220ms_ease]",
  // the list: front at the top, the rest below it
  "data-expanded:[transform:translateX(var(--toast-swipe-movement-x,0px))_translateY(calc(var(--toast-swipe-movement-y,0px)_+_var(--toast-offset-y)_+_var(--toast-index)_*_var(--gap)))]",
  "data-expanded:[background:var(--overlay)] data-expanded:[box-shadow:none]",
  // a new one drops in from above the front; a dismissed one slides back up
  "data-starting-style:opacity-0 data-starting-style:[translate:0_-24px]",
  "data-ending-style:opacity-0 data-ending-style:[translate:0_-24px]",
  // same property order as the base transition: transform, translate, opacity, background, shadow
  "data-ending-style:[transition-timing-function:cubic-bezier(0.4,0,1,1)]",
  "data-ending-style:[transition-duration:320ms,140ms,140ms,220ms,220ms]",
  "data-limited:opacity-0",
  // keeps the pointer inside the stack while it crosses the gap between two toasts of the open list
  "after:absolute after:top-full after:left-0 after:h-[calc(var(--gap)_+_1px)] after:w-full after:content-['']",
);

const CONTENT = classes(
  "box-border flex h-full items-center gap-3 py-0 pr-[34px] pl-4",
  "[transition:opacity_220ms_ease]",
  // only the front one is read while stacked; the rest are slivers
  "data-behind:not-data-expanded:opacity-0",
);

// Barra opcional que se vacía durante la vida del aviso; al pausar Base UI su temporizador, también se pausa la barra.
const PROGRESS_TRACK = classes(
  "pointer-events-none absolute inset-0 overflow-hidden rounded-(--tst-r) motion-reduce:hidden",
);

const PROGRESS = classes(
  "animate-tst-progress absolute right-0 bottom-0 left-0 h-1 origin-left bg-primary",
  "group-hover/tst:[animation-play-state:paused] group-focus-within/tst:[animation-play-state:paused]",
);

const ICON = classes(
  "grid flex-none place-items-center",
  "group-data-[kind=success]/toast:text-[#1f9d55]",
  "group-data-[kind=error]/toast:text-destructive",
  "group-data-[kind=warning]/toast:text-[#e09a12]",
  "group-data-[kind=info]/toast:text-(--tst-mute)",
);

const TEXT = "flex min-w-0 flex-1 flex-col gap-0.5";

const TITLE =
  "m-0 truncate text-[14px] leading-[18px] font-medium tracking-[-0.01em]";

const DESCRIPTION =
  "m-0 truncate text-[13px] leading-[17px] tracking-[0em] text-(--tst-mute)";

const ACTION = classes(
  "h-7 flex-none cursor-pointer rounded-[999px] border-0 bg-(--tst-wash) px-3 py-0 text-[13px] font-medium tracking-[0em] text-inherit",
  "[transition:scale_160ms_cubic-bezier(0.22,1,0.36,1)] active:[scale:0.96]",
  "focus-visible:[box-shadow:0_0_0_2px_var(--primary)] focus-visible:[outline:none]",
);

const CLOSE = classes(
  "absolute top-2 right-2 grid size-5 cursor-pointer place-items-center rounded-[999px] border-0 bg-transparent text-(--tst-mute)",
  "[transition:background-color_160ms_ease,color_160ms_ease]",
  "[&:hover]:bg-(--tst-wash) [&:hover]:text-overlay-foreground",
  "focus-visible:[box-shadow:0_0_0_2px_var(--primary)] focus-visible:[outline:none]",
);

const ICONS: Record<NotificationType, IconSvgElement> = {
  success: CheckmarkCircle01Icon,
  info: InformationCircleIcon,
  error: CancelCircleIcon,
  warning: Alert01Icon,
};

// Con data.progress el aviso muestra una barra que se vacía durante su tiempo de vida.
function wantsProgress(data: unknown) {
  return (
    typeof data === "object" &&
    data !== null &&
    "progress" in data &&
    data.progress === true
  );
}

function isNotificationType(
  type: string | undefined,
): type is NotificationType {
  return type !== undefined && type in ICONS;
}

function ToastList() {
  const { toasts } = Toast.useToastManager();
  return (
    <Toast.Portal>
      <Toast.Viewport className={VIEWPORT} aria-label="Notificaciones">
        {toasts.map((t) => {
          const icon = ICONS[isNotificationType(t.type) ? t.type : "info"];
          return (
            <Toast.Root
              key={t.id}
              toast={t}
              swipeDirection="up"
              className={TOAST}
              data-kind={t.type}
            >
              <Toast.Content className={CONTENT}>
                <span className={ICON}>
                  <HugeiconsIcon
                    icon={icon}
                    className="size-4.5"
                    aria-hidden="true"
                  />
                </span>
                <span className={TEXT}>
                  <Toast.Title className={TITLE} />
                  <Toast.Description className={DESCRIPTION} />
                </span>
                {t.actionProps && <Toast.Action className={ACTION} />}
                <Toast.Close className={CLOSE} aria-label="Descartar aviso">
                  <HugeiconsIcon
                    icon={Cancel01Icon}
                    className="size-3"
                    aria-hidden="true"
                  />
                </Toast.Close>
              </Toast.Content>
              {wantsProgress(t.data) && t.type !== "loading" ? (
                <span className={PROGRESS_TRACK} aria-hidden="true">
                  <span
                    className={PROGRESS}
                    style={{
                      animationDuration: `${t.timeout ?? DEFAULT_TOAST_TIMEOUT}ms`,
                    }}
                  />
                </span>
              ) : null}
            </Toast.Root>
          );
        })}
      </Toast.Viewport>
    </Toast.Portal>
  );
}

// Se monta una sola vez en la raíz, para que el aviso sobreviva a la navegación.
export function ToastStack() {
  return (
    <Toast.Provider
      toastManager={toastManager}
      limit={3}
      timeout={DEFAULT_TOAST_TIMEOUT}
    >
      <ToastList />
    </Toast.Provider>
  );
}
