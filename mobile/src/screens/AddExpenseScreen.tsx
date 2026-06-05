import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { api, ApiClientError } from '../api/client';
import { Button } from '../components/Button';
import { DateInput } from '../components/DateInput';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { MainStackParamList } from '../navigation/types';
import { toApiExpenseDate, todayIsoDate, validateExpenseDateInput } from '../utils/expenseDate';

type Props = NativeStackScreenProps<MainStackParamList, 'AddExpense'>;

export function AddExpenseScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [expenseDate, setExpenseDate] = useState(todayIsoDate);
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
      await api.createExpense(
        {
          groupId,
          description: description.trim(),
          amount: parsed,
          expenseDate: toApiExpenseDate(expenseDate),
        },
        token,
        memberId,
      );
      showSuccess('Expense submitted for approval');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to submit expense');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen
      title="Member expense"
      subtitle="Reimbursement for money you paid — admin approval adds credit to your ledger">
      <DateInput value={expenseDate} onChangeText={setExpenseDate} />
      <Input label="Description" value={description} onChangeText={setDescription} />
      <Input
        label="Amount (₹)"
        value={amount}
        onChangeText={setAmount}
        keyboardType="decimal-pad"
      />
      <Button label="Submit expense" onPress={onSubmit} loading={loading} />
    </Screen>
  );
}
