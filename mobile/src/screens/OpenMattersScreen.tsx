import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { OpenMatterResponse, OpenMatterStatus } from '../api/types';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth, useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'OpenMatters'>;

const STATUS_FILTERS: Array<OpenMatterStatus | 'All'> = ['All', 'Open', 'Finalized', 'Cancelled'];

export function OpenMattersScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { canManageMeetings } = useAuth();
  const { showSuccess, showError } = useToast();
  const [filter, setFilter] = useState<OpenMatterStatus | 'All'>('Open');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [adding, setAdding] = useState(false);

  const { data: summary } = useAsyncData(
    useCallback(() => api.getOpenMattersSummary(groupId, token, memberId), [groupId, token, memberId]),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load summary' },
  );

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => {
      const status = filter === 'All' ? undefined : filter;
      return api.getOpenMatters(groupId, token, memberId, status);
    }, [groupId, token, memberId, filter]),
    [groupId, token, memberId, filter],
    { errorMessage: 'Failed to load open matters', loadOnFocus: true },
  );

  const matters = data ?? [];

  const handleAdd = async () => {
    if (!title.trim()) {
      showError('Enter a title');
      return;
    }
    setAdding(true);
    try {
      await api.createOpenMatter(
        groupId,
        { title: title.trim(), description: description.trim() || null },
        token,
        memberId,
      );
      showSuccess('Open matter added');
      setTitle('');
      setDescription('');
      await refresh();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to add open matter');
    } finally {
      setAdding(false);
    }
  };

  const ListHeader = (
    <View style={styles.listHeader}>
      {summary ? (
        <Text style={styles.summaryText}>
          {summary.openCount} open · {summary.finalizedCount} finalized · {summary.cancelledCount}{' '}
          cancelled
        </Text>
      ) : null}
      <View style={styles.filterRow}>
        {STATUS_FILTERS.map((s) => (
          <Pressable
            key={s}
            onPress={() => setFilter(s)}
            style={[styles.filterChip, filter === s && styles.filterChipSelected]}>
            <Text style={[styles.filterText, filter === s && styles.filterTextSelected]}>{s}</Text>
          </Pressable>
        ))}
      </View>
      {canManageMeetings ? (
        <>
          <Input label="New open matter" value={title} onChangeText={setTitle} placeholder="Topic title" />
          <Input
            label="Description"
            value={description}
            onChangeText={setDescription}
            placeholder="Optional details"
            multiline
          />
          <Button
            label={adding ? 'Adding…' : 'Add to backlog'}
            onPress={() => void handleAdd()}
            disabled={adding}
          />
        </>
      ) : null}
    </View>
  );

  return (
    <Screen title="Open matters" subtitle="Group discussion backlog" scroll={false}>
      <ListScreen<OpenMatterResponse>
        data={matters}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No matters in this filter"
        emptyMessage={
          canManageMeetings
            ? 'Add group topics that can be picked for future meeting agendas.'
            : 'Open matters will appear here when the committee adds them.'
        }
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <View style={styles.card}>
            <View style={styles.cardTop}>
              <Text style={styles.cardTitle}>{item.title}</Text>
              <StatusBadge label={formatEnumLabel(item.status)} />
            </View>
            {item.description ? <Text style={styles.cardDesc}>{item.description}</Text> : null}
            <Text style={styles.cardMeta}>Raised {new Date(item.raisedAt).toLocaleDateString('en-IN')}</Text>
          </View>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  listHeader: { marginBottom: spacing.md, gap: spacing.sm },
  summaryText: { fontSize: 12, color: colors.textMuted, fontWeight: '600' },
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
  cardTop: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: spacing.sm },
  cardTitle: { flex: 1, fontSize: 15, fontWeight: '700', color: colors.text },
  cardDesc: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
  cardMeta: { fontSize: 11, color: colors.textLight },
});
