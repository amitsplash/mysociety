import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useEffect, useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { AssetCategory, AssetStatus } from '../api/types';
import { Button } from '../components/Button';
import { DateInput } from '../components/DateInput';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { SelectDropdown, SelectOption } from '../components/SelectDropdown';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { MainStackParamList } from '../navigation/types';
import { colors, spacing } from '../theme';
import { toApiExpenseDate, todayIsoDate } from '../utils/expenseDate';

type Props = NativeStackScreenProps<MainStackParamList, 'AddEditAsset'>;

const CATEGORY_OPTIONS: SelectOption[] = [
  { label: 'Lift', value: 'Lift' },
  { label: 'Generator', value: 'Generator' },
  { label: 'Water pump', value: 'WaterPump' },
  { label: 'Electrical', value: 'Electrical' },
  { label: 'HVAC', value: 'Hvac' },
  { label: 'Plumbing', value: 'Plumbing' },
  { label: 'Security', value: 'Security' },
  { label: 'Fire safety', value: 'FireSafety' },
  { label: 'Other', value: 'Other' },
];

const STATUS_OPTIONS: SelectOption[] = [
  { label: 'Active', value: 'Active' },
  { label: 'Inactive', value: 'Inactive' },
];

const INTERVAL_OPTIONS: SelectOption[] = [
  { label: 'Monthly (30 days)', value: '30' },
  { label: 'Quarterly (90 days)', value: '90' },
  { label: 'Half-yearly (180 days)', value: '180' },
  { label: 'Yearly (365 days)', value: '365' },
];

export function AddEditAssetScreen({ navigation, route }: Props) {
  const assetId = route.params?.assetId;
  const isEdit = Boolean(assetId);
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();

  const [name, setName] = useState('');
  const [category, setCategory] = useState<AssetCategory>('Other');
  const [status, setStatus] = useState<AssetStatus>('Active');
  const [location, setLocation] = useState('');
  const [description, setDescription] = useState('');
  const [serialNumber, setSerialNumber] = useState('');
  const [vendorName, setVendorName] = useState('');
  const [installDate, setInstallDate] = useState('');
  const [intervalDays, setIntervalDays] = useState('90');
  const [alertLeadDays, setAlertLeadDays] = useState('7');
  const [loading, setLoading] = useState(false);
  const [loadingAsset, setLoadingAsset] = useState(isEdit);

  useEffect(() => {
    if (!assetId) return;

    let cancelled = false;
    (async () => {
      try {
        const asset = await api.getAsset(groupId, assetId, token, memberId);
        if (cancelled) return;
        setName(asset.name);
        setCategory(asset.category);
        setStatus(asset.status === 'Decommissioned' ? 'Inactive' : asset.status);
        setLocation(asset.location ?? '');
        setDescription(asset.description ?? '');
        setSerialNumber(asset.serialNumber ?? '');
        setVendorName(asset.vendorName ?? '');
        setInstallDate(asset.installDate ? asset.installDate.slice(0, 10) : '');
        setIntervalDays(String(asset.maintenanceIntervalDays));
        setAlertLeadDays(String(asset.alertLeadDays));
      } catch (e) {
        if (!cancelled) {
          showError(e instanceof ApiClientError ? e.message : 'Failed to load asset');
          navigation.goBack();
        }
      } finally {
        if (!cancelled) setLoadingAsset(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [assetId, groupId, token, memberId, navigation, showError]);

  const onSubmit = async () => {
    const interval = Number(intervalDays);
    const lead = Number(alertLeadDays);
    if (!name.trim() || !interval || interval <= 0 || lead < 0) {
      showError('Enter a name, maintenance interval, and alert lead days');
      return;
    }

    setLoading(true);
    try {
      const body = {
        name: name.trim(),
        category,
        location: location.trim() || null,
        description: description.trim() || null,
        serialNumber: serialNumber.trim() || null,
        vendorName: vendorName.trim() || null,
        installDate: installDate ? toApiExpenseDate(installDate) : null,
        status,
        maintenanceIntervalDays: interval,
        alertLeadDays: lead,
      };

      if (isEdit && assetId) {
        await api.updateAsset(assetId, body, token, memberId);
        showSuccess('Asset updated');
      } else {
        await api.createAsset({ groupId, ...body }, token, memberId);
        showSuccess('Asset added');
      }
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to save asset');
    } finally {
      setLoading(false);
    }
  };

  if (loadingAsset) {
    return <Screen title={isEdit ? 'Edit asset' : 'Add asset'} subtitle="Loading..." />;
  }

  return (
    <Screen
      title={isEdit ? 'Edit asset' : 'Add asset'}
      subtitle="Track equipment and preventive maintenance schedule">
      <Text style={styles.hint}>
        Set how often service is due. Admins receive in-app alerts before and after the due date.
      </Text>
      <Input label="Asset name" value={name} onChangeText={setName} placeholder="Lift A" />
      <SelectDropdown
        label="Category"
        value={category}
        options={CATEGORY_OPTIONS}
        onChange={(value) => setCategory(value as AssetCategory)}
      />
      <Input
        label="Location"
        value={location}
        onChangeText={setLocation}
        placeholder="Tower A basement"
      />
      <Input
        label="Description"
        value={description}
        onChangeText={setDescription}
        placeholder="Optional notes"
        multiline
      />
      <Input
        label="Serial number"
        value={serialNumber}
        onChangeText={setSerialNumber}
        placeholder="Optional"
      />
      <Input
        label="Vendor / manufacturer"
        value={vendorName}
        onChangeText={setVendorName}
        placeholder="Optional"
      />
      <DateInput
        label="Install date (optional)"
        value={installDate || todayIsoDate}
        onChangeText={setInstallDate}
      />
      <SelectDropdown
        label="Maintenance interval"
        value={intervalDays}
        options={INTERVAL_OPTIONS}
        onChange={setIntervalDays}
      />
      <Input
        label="Alert lead days"
        value={alertLeadDays}
        onChangeText={setAlertLeadDays}
        keyboardType="number-pad"
        placeholder="7"
      />
      <SelectDropdown
        label="Status"
        value={status}
        options={STATUS_OPTIONS}
        onChange={(value) => setStatus(value as AssetStatus)}
      />
      <Button label={isEdit ? 'Save changes' : 'Add asset'} onPress={onSubmit} loading={loading} />
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
