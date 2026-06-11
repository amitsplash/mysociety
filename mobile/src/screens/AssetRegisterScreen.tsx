import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { AssetMaintenanceStatus, AssetResponse } from '../api/types';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth, useSession } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatDateShort, formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'AssetRegister'>;

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

export function AssetRegisterScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { isAdmin } = useAuth();

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(
      () => api.getAssets(groupId, token, memberId),
      [groupId, token, memberId],
    ),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load assets' },
  );

  const assets = data ?? [];

  const ListHeader = isAdmin ? (
    <Pressable
      style={({ pressed }) => [styles.addBtn, pressed && styles.addBtnPressed]}
      onPress={() => navigation.navigate('AddEditAsset')}>
      <Text style={styles.addBtnText}>+ Add asset</Text>
    </Pressable>
  ) : null;

  return (
    <Screen
      title="Asset register"
      subtitle="Equipment and preventive maintenance"
      scroll={false}>
      <ListScreen<AssetResponse>
        data={assets}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        emptyTitle="No assets yet"
        emptyBody={
          isAdmin
            ? 'Add lifts, generators, pumps, and other society assets to track maintenance.'
            : 'Your group has not registered any assets yet.'
        }
        renderItem={({ item }) => {
          const badge = maintenanceBadge(item.maintenanceStatus);
          return (
            <Pressable
              style={({ pressed }) => [styles.card, pressed && styles.cardPressed]}
              onPress={() => navigation.navigate('AssetDetail', { assetId: item.id })}>
              <View style={styles.cardTop}>
                <Text style={styles.cardTitle}>{item.name}</Text>
                <StatusBadge label={badge.label} variant={badge.variant} compact />
              </View>
              <Text style={styles.cardMeta}>
                {formatEnumLabel(item.category)}
                {item.location ? ` · ${item.location}` : ''}
              </Text>
              {item.nextDueDate ? (
                <Text style={styles.cardDue}>
                  Next due {formatDateShort(item.nextDueDate)}
                </Text>
              ) : null}
            </Pressable>
          );
        }}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  addBtn: {
    alignSelf: 'flex-start',
    marginBottom: spacing.md,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderRadius: radii.md,
    backgroundColor: colors.primaryMuted,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
  },
  addBtnPressed: { opacity: 0.85 },
  addBtnText: { color: colors.primary, fontWeight: '600', fontSize: 14 },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  cardPressed: { opacity: 0.92 },
  cardTop: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.sm,
    marginBottom: spacing.xs,
  },
  cardTitle: { color: colors.text, fontSize: 16, fontWeight: '600', flex: 1 },
  cardMeta: { color: colors.textMuted, fontSize: 13 },
  cardDue: { color: colors.textLight, fontSize: 12, marginTop: spacing.xs },
});
