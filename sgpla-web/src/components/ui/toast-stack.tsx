import { Toast } from "@base-ui/react/toast";
import {
  CheckmarkCircleFilled,
  DismissCircleFilled,
  DismissRegular,
  InfoFilled,
  WarningFilled,
  type FluentIcon,
} from "@fluentui/react-icons";

/* Adapted from Bencho's "Toast stack" (MIT, bencho.dev/licence), rebuilt on
   Base UI's Toast: it owns the queue, the auto-dismiss timers, pausing them
   on hover or focus, swipe to dismiss and the screen-reader announcements.
   The look and the motion are Bencho's, in index.css.

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

type TipoToast = "success" | "info" | "error" | "warning";

const ICONO: Record<TipoToast, FluentIcon> = {
  success: CheckmarkCircleFilled,
  info: InfoFilled,
  error: DismissCircleFilled,
  warning: WarningFilled,
};

// Manager global: permite avisar desde hooks y callbacks sin depender del contexto de React.
const toastManager = Toast.createToastManager();

function avisar(tipo: TipoToast) {
  return (title: string, description?: string) =>
    toastManager.add({ type: tipo, title, description });
}

export const toast = {
  success: avisar("success"),
  info: avisar("info"),
  error: avisar("error"),
  warning: avisar("warning"),
};

function esTipo(tipo: string | undefined): tipo is TipoToast {
  return tipo !== undefined && tipo in ICONO;
}

function ListaDeToasts() {
  const { toasts } = Toast.useToastManager();
  return (
    <Toast.Portal>
      <Toast.Viewport className="tst" aria-label="Notificaciones">
        {toasts.map((t) => {
          const Icon = ICONO[esTipo(t.type) ? t.type : "info"];
          return (
            <Toast.Root
              key={t.id}
              toast={t}
              swipeDirection="up"
              className="tst-toast"
              data-kind={t.type}
            >
              <Toast.Content className="tst-content">
                <span className="tst-icon">
                  <Icon className="size-[18px]" aria-hidden="true" />
                </span>
                <span className="tst-text">
                  <Toast.Title className="tst-title" />
                  <Toast.Description className="tst-desc" />
                </span>
                {t.actionProps && <Toast.Action className="tst-act" />}
                <Toast.Close className="tst-x" aria-label="Descartar aviso">
                  <DismissRegular className="size-4" aria-hidden="true" />
                </Toast.Close>
              </Toast.Content>
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
    <Toast.Provider toastManager={toastManager} limit={3} timeout={4000}>
      <ListaDeToasts />
    </Toast.Provider>
  );
}
