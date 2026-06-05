import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { GroupDecisionFilter, GroupDecisionResponse } from '../api/types';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useSession } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatCurrency, formatDate } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'Resolutions'>;

const FILTERS: GroupDecisionFilter[] = ['All', 'HasBudget'];

export function ResolutionsScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const [filter, setFilter] = useState<GroupDecisionFilter>('All');

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(
      () => api.getGroupDecisions(groupId, token, memberId, filter),
      [groupId, token, memberId, filter],
    ),
    [groupId, token, memberId, filter],
    { errorMessage: 'Failed to load group decisions', loadOnFocus: true },
  );

  const decisions = data ?? [];

  const ListHeader = (
    <View style={styles.listHeader}>
      <Text style={styles.intro}>
        Decisions recorded in meeting minutes appear here after the meeting is published. Committee
        can preview draft decisions before publish.
      </Text>
      <View style={styles.filterRow}>
        {FILTERS.map((f) => (
          <Pressable
            key={f}
            onPress={() => setFilter(f)}
            style={[styles.filterChip, filter === f && styles.filterChipSelected]}>
            <Text style={[styles.filterText, filter === f && styles.filterTextSelected]}>
              {f === 'All' ? 'All decisions' : 'With budget'}
            </Text>
          </Pressable>
        ))}
      </View>
    </View>
  );

  return (
    <Screen title="Group decisions" subtitle="Published decisions from meetings" scroll={false}>
      <ListScreen<GroupDecisionResponse>
        data={decisions}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No group decisions yet"
        emptyMessage="Record a group decision when finalizing a topic in a meeting, then publish the meeting."
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <Pressable
            onPress={() => navigation.navigate('MeetingDetail', { meetingId: item.meetingId })}
            style={({ pressed }) => [styles.card, pressed && styles.cardPressed]}>
            <View style={styles.cardTop}>
              {item.resolutionNumber ? (
                <Text style={styles.resNumber}>{item.resolutionNumber}</Text>
              ) : (
                <Text style={styles.resNumber}>Decision</Text>
              )}
              <View style={styles.badges}>
                {item.isDraft ? <StatusBadge compact label="Draft" variant="warning" /> : null}
                {item.approvedBudget != null ? (
                  <Text style={styles.budget}>{formatCurrency(item.approvedBudget)}</Text>
                ) : null}
              </View>
            </View>
            <Text style={styles.decisionText} numberOfLines={4}>
              {item.decisionText}
            </Text>
            {item.topicTitle ? <Text style={styles.topic}>Topic: {item.topicTitle}</Text> : null}
            <Text style={styles.meta}>
              {formatDate(item.decidedAt)} · {item.meetingTitle}
            </Text>
          </Pressable>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  listHeader: { marginBottom: spacing.md, gap: spacing.sm },
  intro: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
  filterRow: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  filterChip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.sm,
    paddingHorizontal: 10,
    paddingVertical: 6,
  },
  filterChipSelected: { borderColor: colors.primaryBorder, backgroundColor: colors.primaryMuted },
  filterText: { fontSize: 12, fontWeight: '600', color: colors.textMuted },
  filterTextSelected: { color: colors.primary },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
    gap: spacing.xs,
  },
  cardPressed: { opacity: 0.9 },
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: spacing.sm },
  resNumber: { fontSize: 11, fontWeight: '800', color: colors.primary },
  badges: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs },
  decisionText: { fontSize: 15, fontWeight: '700', color: colors.text, lineHeight: 22 },
  topic: { fontSize: 12, color: colors.info, fontWeight: '600' },
  meta: { fontSize: 11, color: colors.textLight },
  budget: { fontSize: 11, fontWeight: '700', color: colors.success },
});
