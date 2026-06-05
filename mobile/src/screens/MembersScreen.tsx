import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { MemberResponse } from '../api/types';
import { Button } from '../components/Button';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth, useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatCurrency } from '../utils/format';
import { isCorpusPending } from '../utils/fundLedger';

type Props = NativeStackScreenProps<MainStackParamList, 'Members'>;

type MemberRow = MemberResponse & { balance?: number };

export function MembersScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { isAdmin } = useAuth();
  const { showSuccess, showError } = useToast();
  const [markingId, setMarkingId] = useState<string | null>(null);

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(async () => {
      const members = await api.getMembers(groupId, token, memberId);
      if (!isAdmin) {
        return members.map((m) => ({ ...m }));
      }
      const balances = await api.getBalances(groupId, token, memberId);
      const balanceMap = new Map(balances.map((b) => [b.memberId, b.balance]));
      return members.map((m) => ({ ...m, balance: balanceMap.get(m.id) }));
    }, [groupId, token, memberId, isAdmin]),
    [groupId, token, memberId, isAdmin],
    { errorMessage: 'Failed to load members' },
  );

  const members = data ?? [];
  const pendingCorpusCount = members.filter((m) => isCorpusPending(m)).length;

  const handleMarkCorpusReceived = async (id: string) => {
    setMarkingId(id);
    try {
      const result = await api.markCorpusReceived(id, token, memberId);
      showSuccess(
        `Corpus received (${formatCurrency(result.corpusAmountAdded)}). Fund balance: ${formatCurrency(result.corpusFundBalance)}`,
      );
      await refresh();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to mark corpus received');
    } finally {
      setMarkingId(null);
    }
  };

  const subtitle = isAdmin
    ? `${members.length} member${members.length === 1 ? '' : 's'}${
        pendingCorpusCount > 0 ? ` · ${pendingCorpusCount} corpus pending` : ''
      }`
    : `${members.length} member(s)`;

  const ListHeader = isAdmin ? (
    <View style={styles.listHeader}>
      <Button label="Add member" onPress={() => navigation.navigate('AddMember')} />
      {pendingCorpusCount > 0 ? (
        <View style={styles.pendingBanner}>
          <Text style={styles.pendingBannerText}>
            {pendingCorpusCount} member{pendingCorpusCount === 1 ? '' : 's'} with pending corpus
          </Text>
        </View>
      ) : null}
    </View>
  ) : null;

  return (
    <Screen title="Members" subtitle={subtitle} scroll={false}>
      <ListScreen<MemberRow>
        data={members}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No members yet"
        emptyMessage={isAdmin ? 'Tap Add member above to invite your first member.' : 'Ask a group admin to add you.'}
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <MemberCard
            item={item}
            isAdmin={isAdmin}
            markingCorpus={markingId === item.id}
            onMarkCorpusReceived={
              isAdmin && isCorpusPending(item)
                ? () => void handleMarkCorpusReceived(item.id)
                : undefined
            }
            onViewLedger={
              isAdmin
                ? () =>
                    navigation.navigate('Ledger', {
                      memberId: item.id,
                      memberName: item.name,
                    })
                : undefined
            }
            onEdit={
              isAdmin
                ? () =>
                    navigation.navigate('EditMember', {
                      id: item.id,
                      name: item.name,
                      phone: item.phone,
                      role: item.role,
                      squareFeet: item.squareFeet,
                    })
                : undefined
            }
          />
        )}
      />
    </Screen>
  );
}

function MemberCard({
  item,
  isAdmin,
  markingCorpus,
  onMarkCorpusReceived,
  onViewLedger,
  onEdit,
}: {
  item: MemberRow;
  isAdmin: boolean;
  markingCorpus?: boolean;
  onMarkCorpusReceived?: () => void;
  onViewLedger?: () => void;
  onEdit?: () => void;
}) {
  const corpusPending = isCorpusPending(item);

  return (
    <View style={styles.card}>
      <View style={styles.line1}>
        <View style={styles.lineMain}>
          <Text style={styles.name} numberOfLines={1}>
            {item.name}
          </Text>
          <StatusBadge label={item.role} compact />
        </View>
        {onEdit ? (
          <IconActionButton
            icon="create-outline"
            label="Edit member"
            onPress={onEdit}
            variant="indigo"
          />
        ) : null}
      </View>

      <View style={styles.line2}>
        <Text style={styles.phone} numberOfLines={1}>
          {item.phone}
        </Text>
        {isAdmin ? (
          <View style={styles.line2End}>
            {item.balance !== undefined ? (
              <Text
                style={[
                  styles.balance,
                  item.balance >= 0 ? styles.balanceCredit : styles.balanceDebit,
                ]}
                numberOfLines={1}>
                {formatCurrency(item.balance)}
              </Text>
            ) : null}
            {onViewLedger ? (
              <IconActionButton
                icon="book-outline"
                label="View ledger"
                onPress={onViewLedger}
                variant="teal"
              />
            ) : null}
          </View>
        ) : null}
      </View>

      {item.corpusAmount > 0 ? (
        <View style={styles.corpusRow}>
          {corpusPending ? (
            <>
              <StatusBadge label={`Corpus pending ${formatCurrency(item.corpusAmount)}`} variant="warning" />
              {onMarkCorpusReceived ? (
                <Pressable
                  onPress={onMarkCorpusReceived}
                  disabled={markingCorpus}
                  style={({ pressed }) => [
                    styles.markCorpusBtn,
                    pressed && styles.markCorpusBtnPressed,
                    markingCorpus && styles.markCorpusBtnDisabled,
                  ]}>
                  <Text style={styles.markCorpusText}>
                    {markingCorpus ? 'Saving…' : 'Mark received'}
                  </Text>
                </Pressable>
              ) : null}
            </>
          ) : (
            <StatusBadge label={`Corpus paid ${formatCurrency(item.corpusAmount)}`} variant="success" />
          )}
        </View>
      ) : null}
    </View>
  );
}

function IconActionButton({
  icon,
  label,
  onPress,
  variant,
}: {
  icon: keyof typeof Ionicons.glyphMap;
  label: string;
  onPress: () => void;
  variant: 'teal' | 'indigo';
}) {
  const palette =
    variant === 'teal'
      ? { bg: colors.tealMuted, border: 'rgba(45, 212, 191, 0.25)', icon: colors.teal }
      : { bg: colors.primaryMuted, border: colors.primaryBorder, icon: colors.primary };

  return (
    <Pressable
      onPress={onPress}
      accessibilityLabel={label}
      accessibilityRole="button"
      style={({ pressed }) => [
        styles.iconBtn,
        { backgroundColor: palette.bg, borderColor: palette.border },
        pressed && styles.iconBtnPressed,
      ]}>
      <Ionicons name={icon} size={16} color={palette.icon} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  listHeader: {
    marginBottom: spacing.sm,
    gap: spacing.sm,
  },
  pendingBanner: {
    backgroundColor: colors.warningMuted,
    borderRadius: radii.md,
    borderWidth: 1,
    borderColor: colors.warningBorder,
    padding: spacing.sm,
  },
  pendingBannerText: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.warning,
  },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: spacing.sm + 4,
    paddingHorizontal: spacing.md,
    marginBottom: spacing.sm,
  },
  line1: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.sm,
  },
  lineMain: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    minWidth: 0,
  },
  line2: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginTop: 4,
    gap: spacing.sm,
  },
  line2End: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    flexShrink: 0,
  },
  corpusRow: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    gap: spacing.sm,
    marginTop: spacing.sm,
  },
  markCorpusBtn: {
    paddingVertical: 6,
    paddingHorizontal: 10,
    borderRadius: radii.sm,
    backgroundColor: colors.primaryMuted,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
  },
  markCorpusBtnPressed: { opacity: 0.85 },
  markCorpusBtnDisabled: { opacity: 0.6 },
  markCorpusText: {
    fontSize: 12,
    fontWeight: '700',
    color: colors.primary,
  },
  name: {
    flexShrink: 1,
    fontSize: 15,
    fontWeight: '700',
    color: colors.text,
  },
  phone: {
    flex: 1,
    fontSize: 12,
    color: colors.textMuted,
    minWidth: 0,
  },
  iconBtn: {
    width: 32,
    height: 32,
    borderRadius: radii.sm + 2,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconBtnPressed: {
    opacity: 0.85,
    transform: [{ scale: 0.97 }],
  },
  balance: {
    fontSize: 13,
    fontWeight: '700',
    maxWidth: 88,
  },
  balanceCredit: { color: colors.success },
  balanceDebit: { color: colors.danger },
});
