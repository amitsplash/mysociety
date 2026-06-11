import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { api, ApiClientError } from '../api/client';
import { Button } from '../components/Button';
import { DateInput } from '../components/DateInput';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { MainStackParamList } from '../navigation/types';
import { colors, spacing } from '../theme';
import { toApiExpenseDate, todayIsoDate, validateExpenseDateInput } from '../utils/expenseDate';

type Props = NativeStackScreenProps<MainStackParamList, 'AddGroupIncome'>;

const INCOME_EXAMPLES = [
  'Club house booking',
  'Swimming pool booking',
  'Parking fee',
  'Event hall rental',
];

export function AddGroupIncomeScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [incomeDate, setIncomeDate] = useState(todayIsoDate);
  const [loading, setLoading] = useState(false);

  const onSubmit = async () => {
    const parsed = Number(amount);
    const dateError = validateExpenseDateInput(incomeDate);
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
      await api.createGroupIncome(
        {
          groupId,
          description: description.trim(),
          amount: parsed,
          incomeDate: toApiExpenseDate(incomeDate),
        },
        token,
        memberId,
      );
      showSuccess('Maintenance income recorded');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to record income');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen
      title="Record income"
      subtitle="Adds to the maintenance fund (e.g. facility bookings)">
      <Text style={styles.hint}>
        Use this for society income such as clubhouse, swimming pool, or parking fees.
      </Text>
      <DateInput value={incomeDate} onChangeText={setIncomeDate} />
      <Input
        label="Description"
        value={description}
        onChangeText={setDescription}
        placeholder={INCOME_EXAMPLES[0]}
      />
      <Text style={styles.examples}>Examples: {INCOME_EXAMPLES.join(' · ')}</Text>
      <Input
        label="Amount (₹)"
        value={amount}
        onChangeText={setAmount}
        keyboardType="decimal-pad"
      />
      <Button label="Record income" onPress={onSubmit} loading={loading} />
    </Screen>
  );
}

const styles = StyleSheet.create({
  hint: {
    color: colors.textMuted,
    fontSize: 13,
    lineHeight: 18,
    marginBottom: spacing.md,
  },
  examples: {
    color: colors.textLight,
    fontSize: 12,
    lineHeight: 17,
    marginTop: -spacing.sm,
    marginBottom: spacing.md,
  },
});
