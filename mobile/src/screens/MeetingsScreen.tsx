import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { MeetingSummaryResponse } from '../api/types';
import { Button } from '../components/Button';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth, useSession } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatDate, formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'Meetings'>;

export function MeetingsScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { canManageMeetings } = useAuth();

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => api.getMeetings(groupId, token, memberId), [groupId, token, memberId]),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load meetings', loadOnFocus: true },
  );

  const meetings = data ?? [];

  const ListHeader = canManageMeetings ? (
    <View style={styles.listHeader}>
      <Button label="Record meeting" onPress={() => navigation.navigate('AddMeeting')} />
    </View>
  ) : null;

  return (
    <Screen
      title="Meeting minutes"
      subtitle={`${meetings.length} meeting${meetings.length === 1 ? '' : 's'}`}
      scroll={false}>
      <ListScreen<MeetingSummaryResponse>
        data={meetings}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No meetings yet"
        emptyMessage={
          canManageMeetings
            ? 'Record a meeting, then tap backlog topics in the meeting to record discussion.'
            : 'Published meeting minutes will appear here.'
        }
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <Pressable
            onPress={() => navigation.navigate('MeetingDetail', { meetingId: item.id })}
            style={({ pressed }) => [styles.card, pressed && styles.cardPressed]}>
            <View style={styles.cardTop}>
              <Text style={styles.title} numberOfLines={2}>
                {item.title}
              </Text>
              <StatusBadge label={formatEnumLabel(item.status)} />
            </View>
            <Text style={styles.date}>{formatDate(item.meetingDate)}</Text>
            {item.location ? (
              <Text style={styles.meta} numberOfLines={1}>
                {item.location}
              </Text>
            ) : null}
            <Text style={styles.meta}>
              {item.agendaItemCount} agenda item{item.agendaItemCount === 1 ? '' : 's'}
              {item.createdByName ? ` · ${item.createdByName}` : ''}
            </Text>
          </Pressable>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  listHeader: { marginBottom: spacing.sm, gap: spacing.sm },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  cardPressed: { opacity: 0.9 },
  cardTop: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: spacing.sm,
    alignItems: 'flex-start',
  },
  title: { flex: 1, fontSize: 15, fontWeight: '700', color: colors.text },
  date: { marginTop: 4, fontSize: 12, fontWeight: '700', color: colors.primary },
  meta: { marginTop: 4, fontSize: 12, color: colors.textMuted },
});
