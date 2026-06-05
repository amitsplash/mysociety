import { StyleSheet, Text, View } from 'react-native';
import { colors, radii, spacing } from '../theme';
import { formatCurrency } from '../utils/format';

interface SpendingChartProps {
  totalLabel: string;
  totalAmount: number;
  periodLabel?: string;
  bars: { label: string; amount: number }[];
}

export function SpendingChart({
  totalLabel,
  totalAmount,
  periodLabel = 'This period',
  bars,
}: SpendingChartProps) {
  const max = Math.max(...bars.map((b) => b.amount), 1);

  return (
    <View style={styles.wrap}>
      <View style={styles.header}>
        <View>
          <Text style={styles.label}>{totalLabel}</Text>
          <Text style={styles.total}>{formatCurrency(totalAmount)}</Text>
        </View>
        <View style={styles.badge}>
          <Text style={styles.badgeText}>{periodLabel}</Text>
        </View>
      </View>
      <View style={styles.bars}>
        {bars.map((bar) => {
          const barHeight = Math.max(10, Math.round((bar.amount / max) * 64));
          const active = bar.amount === max;
          return (
            <View key={bar.label} style={styles.barCol}>
              <View
                style={[styles.bar, { height: barHeight }, active && styles.barActive]}
              />
            </View>
          );
        })}
      </View>
      <View style={styles.labels}>
        {bars.map((bar) => (
          <Text key={bar.label} style={styles.barLabel}>
            {bar.label}
          </Text>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    borderRadius: radii.xl,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    backgroundColor: 'rgba(99, 102, 241, 0.06)',
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start' },
  label: {
    fontSize: 10,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  total: { fontSize: 24, fontWeight: '900', color: colors.text, marginTop: 2 },
  badge: {
    backgroundColor: colors.primaryMuted,
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: radii.sm,
  },
  badgeText: { fontSize: 10, fontWeight: '700', color: colors.primary },
  bars: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    height: 72,
    gap: 6,
    marginTop: spacing.md,
    paddingHorizontal: 4,
  },
  barCol: { flex: 1, height: '100%', justifyContent: 'flex-end' },
  bar: {
    width: '100%',
    borderTopLeftRadius: 4,
    borderTopRightRadius: 4,
    backgroundColor: colors.surfaceMuted,
    minHeight: 8,
  },
  barActive: { backgroundColor: colors.primary },
  labels: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    marginTop: 6,
    paddingHorizontal: 2,
  },
  barLabel: { flex: 1, textAlign: 'center', fontSize: 9, color: colors.textMuted },
});
