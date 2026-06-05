import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { GroupFundType } from '../api/types';
import { Button } from '../components/Button';
import { DateInput } from '../components/DateInput';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { toApiExpenseDate, todayIsoDate, validateExpenseDateInput } from '../utils/expenseDate';

type Props = NativeStackScreenProps<MainStackParamList, 'AddGroupExpense'>;

const FUND_OPTIONS: { value: GroupFundType; label: string; hint: string }[] = [
  {
    value: 'Maintenance',
    label: 'Maintenance',
    hint: 'Day-to-day group expenses (security, utilities, repairs).',
  },
  {
    value: 'Corpus',
    label: 'Corpus',
    hint: 'Capital works paid from the corpus fund only.',
  },
];

export function AddGroupExpenseScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [expenseDate, setExpenseDate] = useState(todayIsoDate);
  const [fundType, setFundType] = useState<GroupFundType>('Maintenance');
  const [loading, setLoading] = useState(false);

  const onSubmit = async () => {
    const parsed = Number(amount);
    const dateError = validateExpenseDateInput(expenseDate);
    if (!description.trim() || !parsed || parsed <= 0) {
      showError('Enter a description and valid amount');
      return;
    }
    if (dateError) {
      showError(dateError);
      return;
    }
    setLoading(true);
    try {
      await api.createGroupExpense(
        {
          groupId,
          description: description.trim(),
          amount: parsed,
          expenseDate: toApiExpenseDate(expenseDate),
          fundType,
        },
        token,
        memberId,
      );
      showSuccess('Group expense recorded');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to record expense');
    } finally {
      setLoading(false);
    }
  };

  const selectedFund = FUND_OPTIONS.find((f) => f.value === fundType);

  return (
    <Screen
      title="Group expense"
      subtitle="Choose which fund pays for this expense">
      <Text style={styles.fieldLabel}>Pay from</Text>
      <View style={styles.fundRow}>
        {FUND_OPTIONS.map((option) => {
          const active = fundType === option.value;
          return (
            <Pressable
              key={option.value}
              onPress={() => setFundType(option.value)}
              style={[styles.fundChip, active && styles.fundChipActive]}>
              <Text style={[styles.fundChipText, active && styles.fundChipTextActive]}>
                {option.label}
              </Text>
            </Pressable>
          );
        })}
      </View>
      {selectedFund ? <Text style={styles.hint}>{selectedFund.hint}</Text> : null}
      <DateInput value={expenseDate} onChangeText={setExpenseDate} />
      <Input label="Description" value={description} onChangeText={setDescription} />
      <Input
        label="Amount (₹)"
        value={amount}
        onChangeText={setAmount}
        keyboardType="decimal-pad"
      />
      <Button label="Record expense" onPress={onSubmit} loading={loading} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  fieldLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textMuted,
    marginBottom: spacing.xs,
  },
  fundRow: {
    flexDirection: 'row',
    gap: spacing.sm,
    marginBottom: spacing.sm,
  },
  fundChip: {
    flex: 1,
    paddingVertical: spacing.sm,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    alignItems: 'center',
  },
  fundChipActive: {
    borderColor: colors.primaryBorder,
    backgroundColor: colors.primaryMuted,
  },
  fundChipText: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textMuted,
  },
  fundChipTextActive: {
    color: colors.primary,
  },
  hint: {
    color: colors.textMuted,
    fontSize: 12,
    marginBottom: spacing.md,
    lineHeight: 18,
  },
});
