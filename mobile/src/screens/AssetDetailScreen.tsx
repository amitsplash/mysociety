import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { AssetMaintenanceStatus } from '../api/types';
import { Button } from '../components/Button';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth, useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, spacing } from '../theme';
import { confirm } from '../utils/confirm';
import { formatCurrency, formatDateShort, formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'AssetDetail'>;

function maintenanceBadge(status: AssetMaintenanceStatus): {
  label: string;
  variant: 'success' | 'warning' | 'danger' | 'neutral';
} {
  switch (status) {
    case 'Overdue':
      return { label: 'Overdue', variant: 'danger' };
    case 'DueSoon':
      return { label: 'Due soon', variant: 'warning' };
    case 'Ok':
      return { label: 'On schedule', variant: 'success' };
    default:
      return { label: 'Not scheduled', variant: 'neutral' };
  }
}

export function AssetDetailScreen({ navigation, route }: Props) {
  const { assetId } = route.params;
  const { token, memberId, groupId } = useSession();
  const { isAdmin } = useAuth();
  const { showError, showSuccess } = useToast();

  const { data: asset, refresh: refreshAsset } = useAsyncData(
    useCallback(
      () => api.getAsset(groupId, assetId, token, memberId),
      [groupId, assetId, token, memberId],
    ),
    [groupId, assetId, token, memberId],
    { errorMessage: 'Failed to load asset' },
  );

  const { data: records, loading, refreshing, refresh } = useAsyncData(
    useCallback(
      () => api.getMaintenanceRecords(groupId, assetId, token, memberId),
      [groupId, assetId, token, memberId],
    ),
    [groupId, assetId, token, memberId],
    { errorMessage: 'Failed to load maintenance history' },
  );

  const maintenanceRecords = records ?? [];
  const badge = asset ? maintenanceBadge(asset.maintenanceStatus) : null;

  const handleDecommission = async () => {
    if (!asset) return;
    const ok = await confirm(`Decommission "${asset.name}"? Maintenance alerts will stop.`, {
      title: 'Decommission asset',
      confirmLabel: 'Decommission',
    });
    if (!ok) return;

    try {
      await api.decommissionAsset(assetId, token, memberId);
      showSuccess('Asset decommissioned');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to decommission asset');
    }
  };

  const ListHeader = asset ? (
    <View style={styles.header}>
      <SurfaceCard>
        <View style={styles.titleRow}>
          <Text style={styles.name}>{asset.name}</Text>
          {badge ? <StatusBadge label={badge.label} variant={badge.variant} /> : null}
        </View>
        <Text style={styles.meta}>
          {formatEnumLabel(asset.category)} · {formatEnumLabel(asset.status)}
        </Text>
        {asset.location ? <Text style={styles.detail}>Location: {asset.location}</Text> : null}
        {asset.serialNumber ? (
          <Text style={styles.detail}>Serial: {asset.serialNumber}</Text>
        ) : null}
        {asset.vendorName ? <Text style={styles.detail}>Vendor: {asset.vendorName}</Text> : null}
        {asset.installDate ? (
          <Text style={styles.detail}>Installed: {formatDateShort(asset.installDate)}</Text>
        ) : null}
        {asset.nextDueDate ? (
          <Text style={styles.detail}>Next due: {formatDateShort(asset.nextDueDate)}</Text>
        ) : null}
        <Text style={styles.detail}>
          Interval: every {asset.maintenanceIntervalDays} days · Alert {asset.alertLeadDays} days
          before
        </Text>
        {asset.description ? <Text style={styles.description}>{asset.description}</Text> : null}
      </SurfaceCard>

      {isAdmin && asset.status !== 'Decommissioned' ? (
        <View style={styles.actions}>
          <Button
            label="Log maintenance"
            onPress={() =>
              navigation.navigate('LogMaintenance', {
                assetId: asset.id,
                assetName: asset.name,
              })
            }
          />
          <Button
            label="Edit asset"
            variant="secondary"
            onPress={() => navigation.navigate('AddEditAsset', { assetId: asset.id })}
          />
          <Button label="Decommission" variant="danger" onPress={() => void handleDecommission()} />
        </View>
      ) : null}

      <Text style={styles.sectionTitle}>Maintenance history</Text>
    </View>
  ) : null;

  return (
    <Screen scroll={false}>
      <ListScreen
        data={maintenanceRecords}
        loading={loading && !asset}
        refreshing={refreshing}
        onRefresh={async () => {
          await Promise.all([refresh(), refreshAsset()]);
        }}
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        emptyTitle="No maintenance logged"
        emptyBody="Service visits will appear here once recorded."
        renderItem={({ item }) => (
          <View style={styles.recordCard}>
            <Text style={styles.recordDate}>{formatDateShort(item.performedDate)}</Text>
            <Text style={styles.recordDesc}>{item.description}</Text>
            {item.vendorName ? (
              <Text style={styles.recordMeta}>Vendor: {item.vendorName}</Text>
            ) : null}
            {item.cost != null ? (
              <Text style={styles.recordMeta}>Cost: {formatCurrency(item.cost)}</Text>
            ) : null}
            {item.notes ? <Text style={styles.recordNotes}>{item.notes}</Text> : null}
            <Text style={styles.recordBy}>By {item.createdByName}</Text>
          </View>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  header: { marginBottom: spacing.sm },
  titleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.sm,
    marginBottom: spacing.xs,
  },
  name: { color: colors.text, fontSize: 20, fontWeight: '700', flex: 1 },
  meta: { color: colors.textMuted, fontSize: 13, marginBottom: spacing.sm },
  detail: { color: colors.text, fontSize: 14, marginBottom: 4 },
  description: {
    color: colors.textMuted,
    fontSize: 14,
    lineHeight: 20,
    marginTop: spacing.sm,
  },
  actions: { gap: spacing.sm, marginVertical: spacing.md },
  sectionTitle: {
    color: colors.text,
    fontSize: 16,
    fontWeight: '600',
    marginBottom: spacing.sm,
  },
  recordCard: {
    backgroundColor: colors.surface,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  recordDate: { color: colors.primary, fontSize: 13, fontWeight: '600', marginBottom: 4 },
  recordDesc: { color: colors.text, fontSize: 15, marginBottom: 4 },
  recordMeta: { color: colors.textMuted, fontSize: 13 },
  recordNotes: { color: colors.textMuted, fontSize: 13, marginTop: 4, fontStyle: 'italic' },
  recordBy: { color: colors.textLight, fontSize: 12, marginTop: spacing.xs },
});
