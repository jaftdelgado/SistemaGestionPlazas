import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { EASE_OUT, SPRING_PRESS } from "@/lib/ease";
import { cn } from "@/lib/utils";

const CHECK_PATH = "M5 13l4 4L19 7";
const INDETERMINATE_PATH = "M6 12h12";

interface CheckboxProps {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled?: boolean;
  indeterminate?: boolean;
  /** Fuerza el estado de presión cuando el clic ocurre en un área mayor que el control. */
  pressed?: boolean;
  className?: string;
  id?: string;
  tabIndex?: number;
  "aria-label"?: string;
  "aria-describedby"?: string;
}

function Checkbox({
  checked,
  onCheckedChange,
  disabled,
  indeterminate,
  pressed,
  className,
  id,
  tabIndex,
  "aria-label": ariaLabel,
  "aria-describedby": ariaDescribedBy,
}: CheckboxProps) {
  const reduce = useReducedMotion();
  const showMark = checked || indeterminate;
  const path = indeterminate ? INDETERMINATE_PATH : CHECK_PATH;

  return (
    <motion.button
      data-slot="checkbox"
      id={id}
      type="button"
      role="checkbox"
      aria-checked={indeterminate ? "mixed" : checked}
      aria-label={ariaLabel}
      aria-describedby={ariaDescribedBy}
      disabled={disabled}
      tabIndex={tabIndex}
      onClick={() => onCheckedChange(!checked)}
      animate={pressed && !reduce && !disabled ? { scale: 0.92 } : { scale: 1 }}
      whileTap={reduce || disabled ? undefined : { scale: 0.92 }}
      transition={SPRING_PRESS}
      data-state={
        checked ? "checked" : indeterminate ? "indeterminate" : "unchecked"
      }
      className={cn(
        "flex size-4 shrink-0 items-center justify-center rounded-sm border-[1.5px] outline-none transition-colors duration-200 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
        showMark
          ? "border-primary bg-primary text-primary-foreground"
          : "border-ring bg-input",
        className,
      )}
    >
      <AnimatePresence initial={false}>
        {showMark ? (
          <motion.svg
            key={indeterminate ? "indeterminate" : "checked"}
            data-slot="checkbox-indicator"
            width="12"
            height="12"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth={3}
            strokeLinecap="round"
            strokeLinejoin="round"
            initial={reduce ? { opacity: 1 } : { opacity: 0, scale: 0.5 }}
            animate={reduce ? { opacity: 1 } : { opacity: 1, scale: 1 }}
            exit={
              reduce
                ? { opacity: 0 }
                : { opacity: 0, scale: 0.5, filter: "blur(4px)" }
            }
            transition={
              reduce ? { duration: 0 } : { duration: 0.16, ease: EASE_OUT }
            }
            aria-hidden="true"
          >
            <motion.path
              d={path}
              initial={reduce ? { pathLength: 1 } : { pathLength: 0 }}
              animate={{ pathLength: 1 }}
              transition={
                reduce
                  ? { duration: 0 }
                  : {
                      duration: indeterminate ? 0.2 : 0.3,
                      ease: EASE_OUT,
                      delay: 0.04,
                    }
              }
            />
          </motion.svg>
        ) : null}
      </AnimatePresence>
    </motion.button>
  );
}

export { Checkbox };
