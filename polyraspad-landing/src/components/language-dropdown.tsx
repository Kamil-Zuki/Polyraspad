"use client"

import { useEffect, useRef, useState } from "react"
import { ChevronDown, Languages } from "lucide-react"

type LanguageDropdownProps = {
  currentLabel: string
  options: Array<{
    href: string
    label: string
  }>
}

export function LanguageDropdown({
  currentLabel,
  options,
}: LanguageDropdownProps) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    function handlePointerDown(event: PointerEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    function handleEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false)
      }
    }

    document.addEventListener("pointerdown", handlePointerDown)
    document.addEventListener("keydown", handleEscape)

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown)
      document.removeEventListener("keydown", handleEscape)
    }
  }, [])

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="inline-flex items-center gap-2 rounded-lg border border-white/10 px-3 py-2 text-sm font-medium text-gray-300 transition hover:border-white/20 hover:text-white"
      >
        <Languages className="h-4 w-4" />
        {currentLabel}
        <ChevronDown
          className={`h-4 w-4 transition-transform ${open ? "rotate-180" : ""}`}
        />
      </button>

      {open ? (
        <div
          role="menu"
          className="glass-panel absolute right-0 top-12 z-50 min-w-32 rounded-xl p-2 shadow-2xl"
        >
          <div className="flex flex-col gap-1">
            {options.map((option) => (
              <a
                key={option.href}
                href={option.href}
                role="menuitem"
                onClick={() => {
                  const match = option.href.match(/^\/(ru|en)/)
                  if (match) {
                    document.cookie = `locale=${match[1]};path=/;max-age=31536000;SameSite=Lax`
                  }
                  setOpen(false)
                }}
                className="rounded-lg px-3 py-2 text-sm font-medium text-gray-300 transition hover:bg-white/5 hover:text-white"
              >
                {option.label}
              </a>
            ))}
          </div>
        </div>
      ) : null}
    </div>
  )
}
