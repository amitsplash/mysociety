import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { CommitteeMemberResponse, CommitteeRole, MemberResponse } from '../api/types';
import { Button } from '../components/Button';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth, useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { confirm } from '../utils/confirm';
import { formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'CommitteeMembers'>;

const COMMITTEE_ROLES: CommitteeRole[] = [
  'President',
  'VicePresident',
  'Secretary',
  'Treasurer',
  'CommitteeMember',
];

export function CommitteeMembersScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { isAdmin, refreshCommitteeStatus } = useAuth();
  const { showSuccess, showError } = useToast();
  const [members, setMembers] = useState<MemberResponse[]>([]);
  const [selectedMemberId, setSelectedMemberId] = useState<string | null>(null);
  const [selectedRole, setSelectedRole] = useState<CommitteeRole>('Secretary');
  const [adding, setAdding] = useState(false);
  const [removingId, setRemovingId] = useState<string | null>(null);

  useEffect(() => {
    if (!isAdmin) {
      navigation.goBack();
    }
  }, [isAdmin, navigation]);

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => api.getCommitteeMembers(groupId, token, memberId), [groupId, token, memberId]),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load committee members' },
  );

  useEffect(() => {
    void api.getMembers(groupId, token, memberId).then(setMembers).catch(() => setMembers([]));
  }, [groupId, token, memberId]);

  const roster = data ?? [];
  const assignedMemberIds = useMemo(() => new Set(roster.map((entry) => entry.memberId)), [roster]);
  const availableMembers = members.filter((member) => !assignedMemberIds.has(member.id));

  const handleAdd = async () => {
    if (!selectedMemberId) {
      showError('Select a member to add');
      return;
    }
    setAdding(true);
    try {
      await api.createCommitteeMember(
        groupId,
        { memberId: selectedMemberId, role: selectedRole },
        token,
        memberId,
      );
      showSuccess('Committee member added');
      setSelectedMemberId(null);
      await refresh();
      await refreshCommitteeStatus();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to add committee member');
    } finally {
      setAdding(false);
    }
  };

  const handleRemove = async (entry: CommitteeMemberResponse) => {
    const ok = await confirm(`Remove ${entry.memberName} from the committee?`, {
      title: 'Remove committee member',
      confirmLabel: 'Remove',
    });
    if (!ok) return;

    setRemovingId(entry.id);
    try {
      await api.deleteCommitteeMember(groupId, entry.id, token, memberId);
      showSuccess('Committee member removed');
      await refresh();
      await refreshCommitteeStatus();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to remove committee member');
    } finally {
      setRemovingId(null);
    }
  };

  const ListHeader = (
    <View style={styles.listHeader}>
      <Text style={styles.sectionLabel}>Add committee member</Text>
      <View style={styles.pickerBlock}>
        <Text style={styles.fieldLabel}>Member</Text>
        <View style={styles.optionRow}>
          {availableMembers.length === 0 ? (
            <Text style={styles.emptyHint}>All group members are already on the committee.</Text>
          ) : (
            availableMembers.map((member) => (
              <Pressable
                key={member.id}
                onPress={() => setSelectedMemberId(member.id)}
                style={[
                  styles.optionChip,
                  selectedMemberId === member.id && styles.optionChipSelected,
                ]}>
                <Text
                  style={[
                    styles.optionChipText,
                    selectedMemberId === member.id && styles.optionChipTextSelected,
                  ]}>
                  {member.name}
                </Text>
              </Pressable>
            ))
          )}
        </View>
      </View>
      <View style={styles.pickerBlock}>
        <Text style={styles.fieldLabel}>Role</Text>
        <View style={styles.optionRow}>
          {COMMITTEE_ROLES.map((role) => (
            <Pressable
              key={role}
              onPress={() => setSelectedRole(role)}
              style={[styles.optionChip, selectedRole === role && styles.optionChipSelected]}>
              <Text
                style={[
                  styles.optionChipText,
                  selectedRole === role && styles.optionChipTextSelected,
                ]}>
                {formatEnumLabel(role)}
              </Text>
            </Pressable>
          ))}
        </View>
      </View>
      <Button
        label={adding ? 'Adding…' : 'Add to committee'}
        onPress={() => void handleAdd()}
        disabled={adding || !selectedMemberId}
      />
    </View>
  );

  return (
    <Screen title="Committee members" subtitle={`${roster.length} on committee`} scroll={false}>
      <ListScreen<CommitteeMemberResponse>
        data={roster}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No committee members yet"
        emptyMessage="Assign group members to committee roles above."
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardMain}>
              <Text style={styles.name}>{item.memberName}</Text>
              <StatusBadge label={formatEnumLabel(item.role)} variant="info" compact />
            </View>
            <Pressable
              onPress={() => void handleRemove(item)}
              disabled={removingId === item.id}
              accessibilityLabel="Remove committee member"
              style={({ pressed }) => [styles.removeBtn, pressed && styles.removeBtnPressed]}>
              <Ionicons name="trash-outline" size={16} color={colors.danger} />
            </Pressable>
          </View>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  listHeader: {
    marginBottom: spacing.md,
    gap: spacing.sm,
  },
  sectionLabel: {
    fontSize: 13,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.4,
  },
  pickerBlock: { gap: spacing.xs },
  fieldLabel: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
  },
  optionRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  optionChip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.sm,
    paddingHorizontal: 10,
    paddingVertical: 6,
    backgroundColor: colors.surface,
  },
  optionChipSelected: {
    borderColor: colors.primaryBorder,
    backgroundColor: colors.primaryMuted,
  },
  optionChipText: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
  },
  optionChipTextSelected: {
    color: colors.primary,
  },
  emptyHint: {
    fontSize: 12,
    color: colors.textMuted,
  },
  card: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
    gap: spacing.sm,
  },
  cardMain: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    flexWrap: 'wrap',
  },
  name: {
    fontSize: 15,
    fontWeight: '700',
    color: colors.text,
  },
  removeBtn: {
    width: 32,
    height: 32,
    borderRadius: radii.sm,
    borderWidth: 1,
    borderColor: colors.dangerBorder,
    backgroundColor: colors.dangerMuted,
    alignItems: 'center',
    justifyContent: 'center',
  },
  removeBtnPressed: { opacity: 0.85 },
});
