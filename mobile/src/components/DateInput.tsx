import { Input } from './Input';
import { todayIsoDate } from '../utils/expenseDate';

interface DateInputProps {
  label?: string;
  value: string;
  onChangeText: (value: string) => void;
}

export function DateInput({
  label = 'Expense date',
  value,
  onChangeText,
}: DateInputProps) {
  return (
    <Input
      label={label}
      value={value}
      onChangeText={onChangeText}
      placeholder={todayIsoDate()}
      autoCapitalize="none"
      autoCorrect={false}
    />
  );
}
