import { StyleSheet, Text, View } from 'react-native';
import { colors, radii } from '../theme';

type BadgeVariant = 'success' | 'warning' | 'danger' | 'neutral' | 'info';

const variantMap: Record<string, BadgeVariant> = {
  Paid: 'success',
  Approved: 'success',
  Partial: 'warning',
  Rejected: 'danger',
  Admin: 'info',
  Member: 'neutral',
  Open: 'neutral',
  Assigned: 'info',
  NeedMoreDiscussion: 'warning',
  Closed: 'success',
  Draft: 'neutral',
  UnderReview: 'warning',
  Published: 'success',
  Archived: 'neutral',
  Finalized: 'success',
  Active: 'info',
  Completed: 'success',
  Postponed: 'warning',
  Discussed: 'info',
  NotDiscussed: 'neutral',
  Cancelled: 'danger',
  FromBacklog: 'info',
  AdHoc: 'neutral',
};

interface StatusBadgeProps {
  label: string;
  variant?: BadgeVariant;
  compact?: boolean;
}

export function StatusBadge({ label, variant, compact }: StatusBadgeProps) {
  const v = variant ?? variantMap[label] ?? 'neutral';
  return (
    <View style={[styles.badge, compact && styles.badgeCompact, bg[v], border[v]]}>
      <Text style={[styles.text, compact && styles.textCompact, text[v]]}>{label}</Text>
    </View>
  );
}

const bg = {
  success: { backgroundColor: colors.successMuted },
  warning: { backgroundColor: colors.warningMuted },
  danger: { backgroundColor: colors.dangerMuted },
  neutral: { backgroundColor: colors.surfaceMuted },
  info: { backgroundColor: colors.infoMuted },
};

const border = {
  success: { borderColor: colors.successBorder },
  warning: { borderColor: colors.warningBorder },
  danger: { borderColor: colors.dangerBorder },
  neutral: { borderColor: colors.border },
  info: { borderColor: colors.primaryBorder },
};

const text = {
  success: { color: colors.success },
  warning: { color: colors.warning },
  danger: { color: colors.danger },
  neutral: { color: colors.textMuted },
  info: { color: colors.info },
};

const styles = StyleSheet.create({
  badge: {
    alignSelf: 'flex-start',
    borderWidth: 1,
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: radii.sm,
    marginTop: 6,
  },
  badgeCompact: {
    marginTop: 0,
    paddingHorizontal: 7,
    paddingVertical: 2,
  },
  text: { fontSize: 10, fontWeight: '800', letterSpacing: 0.3 },
  textCompact: { fontSize: 9 },
});
