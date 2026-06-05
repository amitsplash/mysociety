import { StyleSheet, Text, View, ViewStyle } from 'react-native';
import { colors, radii, spacing } from '../theme';
import { formatCurrency } from '../utils/format';

interface BalanceAmountProps {
  amount: number;
  label?: string;
  size?: 'sm' | 'lg';
  style?: ViewStyle;
}

export function BalanceAmount({ amount, label, size = 'sm', style }: BalanceAmountProps) {
  const isCredit = amount >= 0;
  return (
    <View
      style={[
        styles.wrap,
        isCredit ? styles.credit : styles.debit,
        size === 'lg' && styles.wrapLg,
        style,
      ]}>
      {label ? <Text style={[styles.label, isCredit ? styles.labelCredit : styles.labelDebit]}>{label}</Text> : null}
      <Text style={[styles.amount, size === 'lg' && styles.amountLg, isCredit ? styles.creditText : styles.debitText]}>
        {formatCurrency(amount)}
      </Text>
      <Text style={styles.hint}>{isCredit ? 'Credit balance' : 'Outstanding due'}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    borderRadius: radii.lg,
    padding: spacing.md,
    borderWidth: 1,
    marginBottom: spacing.md,
  },
  wrapLg: { padding: spacing.lg },
  credit: {
    backgroundColor: colors.successBg,
    borderColor: colors.successBorder,
  },
  debit: {
    backgroundColor: colors.dangerBg,
    borderColor: colors.dangerBorder,
  },
  label: { fontSize: 12, fontWeight: '600', marginBottom: 4 },
  labelCredit: { color: colors.success },
  labelDebit: { color: colors.danger },
  amount: { fontSize: 22, fontWeight: '800', letterSpacing: -0.5 },
  amountLg: { fontSize: 32 },
  creditText: { color: colors.success },
  debitText: { color: colors.danger },
  hint: { fontSize: 12, color: colors.textMuted, marginTop: 4 },
});
