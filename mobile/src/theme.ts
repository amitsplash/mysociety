/** ResiConnect-inspired dark palette for Society360 */
export const colors = {
  background: '#020617',
  backgroundElevated: '#0f172a',
  surface: '#0f172a',
  surfaceMuted: '#1e293b',
  surfaceInset: '#020617',
  border: '#1e293b',
  borderSubtle: '#334155',
  borderFocus: '#6366f1',
  text: '#f1f5f9',
  textMuted: '#94a3b8',
  textLight: '#64748b',
  primary: '#6366f1',
  primaryDark: '#4f46e5',
  primaryMuted: 'rgba(99, 102, 241, 0.15)',
  primaryBorder: 'rgba(99, 102, 241, 0.3)',
  success: '#34d399',
  successMuted: 'rgba(52, 211, 153, 0.12)',
  successBorder: 'rgba(52, 211, 153, 0.25)',
  warning: '#fbbf24',
  warningMuted: 'rgba(251, 191, 36, 0.12)',
  warningBorder: 'rgba(251, 191, 36, 0.25)',
  danger: '#fb7185',
  dangerMuted: 'rgba(251, 113, 133, 0.12)',
  dangerBorder: 'rgba(251, 113, 133, 0.25)',
  teal: '#2dd4bf',
  tealMuted: 'rgba(45, 212, 191, 0.12)',
  info: '#818cf8',
  infoMuted: 'rgba(129, 140, 248, 0.12)',
  tabBar: '#020617',
  tabBarBorder: '#0f172a',
  shadow: 'rgba(99, 102, 241, 0.15)',
  gradientStart: 'rgba(99, 102, 241, 0.1)',
  gradientEnd: 'rgba(45, 212, 191, 0.08)',
  /** Legacy aliases used by ledger screens */
  primaryLight: 'rgba(99, 102, 241, 0.15)',
  successBg: 'rgba(52, 211, 153, 0.12)',
  dangerBg: 'rgba(251, 113, 133, 0.12)',
};

export const spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
};

export const radii = {
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
  xxl: 24,
};

export const typography = {
  title: { fontSize: 26, fontWeight: '800' as const, letterSpacing: -0.5 },
  heading: { fontSize: 17, fontWeight: '800' as const, letterSpacing: -0.3 },
  section: {
    fontSize: 11,
    fontWeight: '700' as const,
    letterSpacing: 0.8,
    textTransform: 'uppercase' as const,
  },
};

export type AccentTone = 'indigo' | 'teal' | 'emerald' | 'rose' | 'amber';

export const accentTones: Record<
  AccentTone,
  { bg: string; text: string; border: string }
> = {
  indigo: {
    bg: colors.primaryMuted,
    text: colors.primary,
    border: colors.primaryBorder,
  },
  teal: { bg: colors.tealMuted, text: colors.teal, border: 'rgba(45, 212, 191, 0.25)' },
  emerald: {
    bg: colors.successMuted,
    text: colors.success,
    border: colors.successBorder,
  },
  rose: { bg: colors.dangerMuted, text: colors.danger, border: colors.dangerBorder },
  amber: {
    bg: colors.warningMuted,
    text: colors.warning,
    border: colors.warningBorder,
  },
};
