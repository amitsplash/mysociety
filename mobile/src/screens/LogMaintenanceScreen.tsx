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

type Props = NativeStackScreenProps<MainStackParamList, 'LogMaintenance'>;

export function LogMaintenanceScreen({ navigation, route }: Props) {
  const { assetId, assetName } = route.params;
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();

  const [description, setDescription] = useState('');
  const [performedDate, setPerformedDate] = useState(todayIsoDate);
  const [cost, setCost] = useState('');
  const [vendorName, setVendorName] = useState('');
  const [notes, setNotes] = useState('');
  const [loading, setLoading] = useState(false);

  const onSubmit = async () => {
    const dateError = validateExpenseDateInput(performedDate);
    if (!description.trim()) {
      showError('Enter what maintenance was performed');
      return;
    }
    if (dateError) {
      showError(dateError);
      return;
    }

    const parsedCost = cost.trim() ? Number(cost) : null;
    if (cost.trim() && (!parsedCost || parsedCost < 0)) {
      showError('Enter a valid cost or leave blank');
      return;
    }

    setLoading(true);
    try {
      await api.createMaintenanceRecord(
        {
          assetId,
          groupId,
          performedDate: toApiExpenseDate(performedDate),
          description: description.trim(),
          cost: parsedCost,
          vendorName: vendorName.trim() || null,
          notes: notes.trim() || null,
        },
        token,
        memberId,
      );
      showSuccess('Maintenance logged');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to log maintenance');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen
      title="Log maintenance"
      subtitle={assetName}
      >
      <Text style={styles.hint}>
        Recording service resets the next due date based on the asset maintenance interval.
      </Text>
      <DateInput value={performedDate} onChangeText={setPerformedDate} />
      <Input
        label="Work performed"
        value={description}
        onChangeText={setDescription}
        placeholder="Oil change, belt replacement, inspection..."
        multiline
      />
      <Input
        label="Vendor (optional)"
        value={vendorName}
        onChangeText={setVendorName}
        placeholder="Service company name"
      />
      <Input
        label="Cost (₹, optional)"
        value={cost}
        onChangeText={setCost}
        keyboardType="decimal-pad"
      />
      <Input
        label="Notes (optional)"
        value={notes}
        onChangeText={setNotes}
        placeholder="Additional details"
        multiline
      />
      <Button label="Save maintenance record" onPress={onSubmit} loading={loading} />
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
});
