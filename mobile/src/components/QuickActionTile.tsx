import { Pressable, StyleSheet, Text, View } from 'react-native';
import { AccentTone, colors, radii, spacing } from '../theme';
import { AppIcon, IconName } from './AppIcon';

type MetricTone = 'default' | 'warning' | 'success' | 'danger';

interface QuickActionTileProps {
  title: string;
  subtitle: string;
  metric?: string;
  metricTone?: MetricTone;
  icon: IconName;
  tone?: AccentTone;
  onPress: () => void;
}

const metricColors: Record<MetricTone, string> = {
  default: colors.text,
  warning: colors.warning,
  success: colors.success,
  danger: colors.danger,
};

export function QuickActionTile({
  title,
  subtitle,
  metric,
  metricTone = 'default',
  icon,
  tone = 'indigo',
  onPress,
}: QuickActionTileProps) {
  return (
    <Pressable
      onPress={onPress}
      style={({ pressed }) => [styles.tile, pressed && styles.pressed]}>
      <View style={styles.titleRow}>
        <AppIcon name={icon} tone={tone} size={16} style={styles.icon} />
        <Text style={styles.title} numberOfLines={1}>
          {title}
        </Text>
      </View>
      {metric ? (
        <Text style={[styles.metric, { color: metricColors[metricTone] }]}>{metric}</Text>
      ) : null}
      <Text style={styles.subtitle} numberOfLines={1}>
        {subtitle}
      </Text>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  tile: {
    flex: 1,
    minWidth: '46%',
    backgroundColor: colors.surface,
    borderRadius: radii.xl,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md - 2,
    gap: spacing.sm,
  },
  pressed: { opacity: 0.92, borderColor: colors.borderFocus },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
  },
  icon: {
    width: 28,
    height: 28,
    borderRadius: 10,
  },
  title: {
    flex: 1,
    fontSize: 12,
    fontWeight: '700',
    color: colors.text,
  },
  metric: { fontSize: 18, fontWeight: '800', letterSpacing: -0.3 },
  subtitle: { fontSize: 10, color: colors.textMuted, marginTop: -2 },
});
