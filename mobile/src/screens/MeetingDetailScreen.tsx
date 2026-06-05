import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useMemo, useState } from 'react';
import {
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { api, ApiClientError } from '../api/client';
import type {
  AgendaItemResponse,
  MeetingItemOutcome,
  OpenMatterResponse,
  GroupDecisionResponse,
} from '../api/types';
import { MeetingTopicSheet, type TopicSheetDraft } from '../components/meeting/MeetingTopicSheet';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { StatusBadge } from '../components/StatusBadge';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth, useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { confirm } from '../utils/confirm';
import { formatCurrency, formatDate, formatEnumLabel } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'MeetingDetail'>;

type ListFilter = 'all' | 'pending' | 'discussed';

type ActiveTopic =
  | { kind: 'backlog'; matter: OpenMatterResponse; agendaItem: AgendaItemResponse | null }
  | { kind: 'adhoc'; agendaItem: AgendaItemResponse };

function draftFromAgenda(item: AgendaItemResponse | null): TopicSheetDraft {
  if (!item) {
    return { discussion: '', decision: '', budget: '', outcome: 'NotDiscussed' };
  }
  return {
    discussion: item.discussionSummary ?? item.minute?.discussionSummary ?? '',
    decision: item.minute?.decisionTaken ?? '',
    budget: item.minute?.budgetApproved != null ? String(item.minute.budgetApproved) : '',
    outcome: item.outcome,
  };
}

function isDiscussed(outcome: MeetingItemOutcome) {
  return outcome !== 'NotDiscussed';
}

function hasDecision(agendaItem: AgendaItemResponse | null | undefined) {
  return Boolean(agendaItem?.minute?.decisionTaken?.trim());
}

export function MeetingDetailScreen({ route }: Props) {
  const { meetingId } = route.params;
  const { token, memberId, groupId } = useSession();
  const { canManageMeetings } = useAuth();
  const { showSuccess, showError } = useToast();
  const [search, setSearch] = useState('');
  const [listFilter, setListFilter] = useState<ListFilter>('all');
  const [busy, setBusy] = useState(false);
  const [activeTopic, setActiveTopic] = useState<ActiveTopic | null>(null);
  const [sheetDraft, setSheetDraft] = useState<TopicSheetDraft>(draftFromAgenda(null));
  const [adHocTitle, setAdHocTitle] = useState('');
  const [showAdHocForm, setShowAdHocForm] = useState(false);

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(async () => {
      const [meeting, openMatters] = await Promise.all([
        api.getMeeting(groupId, meetingId, token, memberId),
        api.getOpenMatters(groupId, token, memberId, 'Open').catch(() => [] as OpenMatterResponse[]),
      ]);
      return { meeting, openMatters };
    }, [groupId, meetingId, token, memberId]),
    [groupId, meetingId, token, memberId],
    { errorMessage: 'Failed to load meeting', loadOnFocus: true },
  );

  const meeting = data?.meeting;
  const openMatters = data?.openMatters ?? [];
  const canEdit =
    canManageMeetings && meeting && meeting.status !== 'Published' && meeting.status !== 'Archived';

  const agendaByMatterId = useMemo(() => {
    const map = new Map<string, AgendaItemResponse>();
    for (const item of meeting?.agendaItems ?? []) {
      if (item.openMatterId) map.set(item.openMatterId, item);
    }
    return map;
  }, [meeting?.agendaItems]);

  const adHocItems = useMemo(
    () => (meeting?.agendaItems ?? []).filter((i) => !i.openMatterId),
    [meeting?.agendaItems],
  );

  const backlogRows = useMemo(() => {
    const q = search.trim().toLowerCase();
    return openMatters
      .filter((m) => {
        if (!q) return true;
        return (
          m.title.toLowerCase().includes(q) ||
          (m.description?.toLowerCase().includes(q) ?? false)
        );
      })
      .map((matter) => ({
        matter,
        agendaItem: agendaByMatterId.get(matter.id) ?? null,
      }))
      .filter(({ agendaItem }) => {
        const outcome = agendaItem?.outcome ?? 'NotDiscussed';
        if (listFilter === 'pending') return !isDiscussed(outcome);
        if (listFilter === 'discussed') return isDiscussed(outcome);
        return true;
      });
  }, [openMatters, agendaByMatterId, search, listFilter]);

  const discussedCount = useMemo(() => {
    let n = 0;
    for (const m of openMatters) {
      const item = agendaByMatterId.get(m.id);
      if (item && isDiscussed(item.outcome)) n++;
    }
    n += adHocItems.filter((i) => isDiscussed(i.outcome)).length;
    return n;
  }, [openMatters, agendaByMatterId, adHocItems]);

  const totalTopics = openMatters.length + (canEdit ? adHocItems.length : 0);

  const openSheet = (topic: ActiveTopic) => {
    const agenda =
      topic.kind === 'backlog' ? topic.agendaItem : topic.agendaItem;
    setActiveTopic(topic);
    setSheetDraft(draftFromAgenda(agenda));
  };

  const closeSheet = () => {
    setActiveTopic(null);
    setSheetDraft(draftFromAgenda(null));
  };

  const ensureAgendaForMatter = async (
    matter: OpenMatterResponse,
  ): Promise<AgendaItemResponse> => {
    const existing = agendaByMatterId.get(matter.id);
    if (existing) return existing;
    const created = await api.addAgendaFromOpenMatter(
      groupId,
      meetingId,
      matter.id,
      token,
      memberId,
    );
    return created;
  };

  const handleTopicPress = async (matter: OpenMatterResponse) => {
    if (!canEdit) {
      const item = agendaByMatterId.get(matter.id);
      if (!item) return;
      openSheet({ kind: 'backlog', matter, agendaItem: item });
      return;
    }
    setBusy(true);
    try {
      const agendaItem = await ensureAgendaForMatter(matter);
      await refresh();
      openSheet({ kind: 'backlog', matter, agendaItem });
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Could not open topic');
    } finally {
      setBusy(false);
    }
  };

  const handleAdHocPress = (item: AgendaItemResponse) => {
    openSheet({
      kind: 'adhoc',
      agendaItem: item,
    });
  };

  const handleSaveTopic = async () => {
    if (!activeTopic || !canEdit) return;
    if (sheetDraft.outcome === 'Finalized' && !sheetDraft.decision.trim()) {
      showError('Enter a group decision when status is Finalized');
      return;
    }

    const agendaItem =
      activeTopic.kind === 'backlog'
        ? activeTopic.agendaItem ?? agendaByMatterId.get(activeTopic.matter.id)
        : activeTopic.agendaItem;

    setBusy(true);
    try {
      let item = agendaItem;
      if (!item?.openMatterId && item) {
        await api.promoteAgendaToOpenMatter(groupId, item.id, token, memberId);
        await refresh();
        const refreshed = await api.getMeeting(groupId, meetingId, token, memberId);
        item = refreshed.agendaItems.find((a) => a.id === item!.id) ?? item;
      }

      if (!item) {
        showError('Topic not linked to meeting yet');
        return;
      }
      const budget = sheetDraft.budget.trim() ? Number(sheetDraft.budget) : null;
      await api.upsertMinute(
        groupId,
        item.id,
        {
          discussionSummary: sheetDraft.discussion.trim() || null,
          decisionTaken: sheetDraft.decision.trim() || null,
          budgetApproved: budget != null && !Number.isNaN(budget) ? budget : null,
        },
        token,
        memberId,
      );
      await api.updateAgendaOutcome(
        groupId,
        item.id,
        {
          outcome: sheetDraft.outcome,
          discussionSummary: sheetDraft.discussion.trim() || null,
        },
        token,
        memberId,
      );
      showSuccess('Saved');
      closeSheet();
      await refresh();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to save');
    } finally {
      setBusy(false);
    }
  };

  const handleAddAdHoc = async () => {
    if (!adHocTitle.trim()) {
      showError('Enter a title for the new point');
      return;
    }
    setBusy(true);
    try {
      const item = await api.addAgendaItem(
        groupId,
        meetingId,
        { title: adHocTitle.trim(), source: 'AdHoc' },
        token,
        memberId,
      );
      setAdHocTitle('');
      setShowAdHocForm(false);
      await refresh();
      openSheet({ kind: 'adhoc', agendaItem: item });
      showSuccess('Added to meeting and open matters backlog');
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to add point');
    } finally {
      setBusy(false);
    }
  };

  const handlePublish = async () => {
    const ok = await confirm(
      'Publish this meeting? All members will see discussion and decisions for topics you recorded.',
      { title: 'Publish meeting', confirmLabel: 'Publish' },
    );
    if (!ok) return;
    setBusy(true);
    try {
      await api.updateMeetingStatus(groupId, meetingId, { status: 'Published' }, token, memberId);
      showSuccess('Meeting published');
      await refresh();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to publish');
    } finally {
      setBusy(false);
    }
  };

  const openDecision = (decision: GroupDecisionResponse) => {
    if (!decision.agendaItemId || !meeting) return;
    const item = meeting.agendaItems.find((a) => a.id === decision.agendaItemId);
    if (!item) return;
    openSheet({ kind: 'adhoc', agendaItem: item });
  };

  if (loading && !meeting) {
    return (
      <Screen title="Meeting" subtitle="Loading…">
        <View />
      </Screen>
    );
  }

  if (!meeting) {
    return (
      <Screen title="Meeting" subtitle="Meeting not found">
        <View />
      </Screen>
    );
  }

  const decisions = meeting.decisions ?? [];
  const progress =
    totalTopics > 0 ? Math.round((discussedCount / Math.max(totalTopics, 1)) * 100) : 0;

  const sheetTitle =
    activeTopic?.kind === 'backlog' ? activeTopic.matter.title : activeTopic?.agendaItem.title ?? '';

  const sheetDescription =
    activeTopic?.kind === 'backlog' ? activeTopic.matter.description : null;

  const sheetAgenda =
    activeTopic?.kind === 'backlog'
      ? activeTopic.agendaItem ?? agendaByMatterId.get(activeTopic.matter.id) ?? null
      : activeTopic?.agendaItem ?? null;

  const publishedAgendaItems = (meeting.agendaItems ?? []).filter(
    (i) => isDiscussed(i.outcome) || i.discussionSummary || i.minute?.decisionTaken,
  );

  return (
    <Screen scroll={false}>
      <ScrollView
        contentContainerStyle={styles.scroll}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={refresh} tintColor={colors.primary} />
        }>
        <SurfaceCard>
          <View style={styles.headerRow}>
            <Text style={styles.meetingTitle}>{meeting.title}</Text>
            <StatusBadge label={formatEnumLabel(meeting.status)} />
          </View>
          <Text style={styles.meta}>{formatDate(meeting.meetingDate)}</Text>
          {meeting.location ? <Text style={styles.meta}>{meeting.location}</Text> : null}
          {canEdit ? (
            <>
              <View style={styles.progressRow}>
                <Text style={styles.progressLabel}>
                  {discussedCount} of {openMatters.length} backlog topics discussed
                </Text>
                <Text style={styles.progressPct}>{progress}%</Text>
              </View>
              <View style={styles.progressTrack}>
                <View style={[styles.progressFill, { width: `${progress}%` }]} />
              </View>
              <Button
                label={busy ? 'Publishing…' : 'Publish meeting'}
                onPress={() => void handlePublish()}
                disabled={busy}
              />
            </>
          ) : null}
        </SurfaceCard>

        {decisions.length > 0 ? (
          <>
            <Text style={styles.sectionLabel}>Decisions in this meeting</Text>
            {decisions.map((d) => (
              <Pressable
                key={d.id}
                onPress={() => openDecision(d)}
                style={({ pressed }) => [styles.decisionCard, pressed && styles.topicRowPressed]}>
                {d.resolutionNumber ? (
                  <Text style={styles.resNumber}>{d.resolutionNumber}</Text>
                ) : null}
                <Text style={styles.decisionText}>{d.decisionText}</Text>
                {d.topicTitle ? <Text style={styles.topicSub}>Topic: {d.topicTitle}</Text> : null}
                {d.approvedBudget != null ? (
                  <Text style={styles.budget}>{formatCurrency(d.approvedBudget)}</Text>
                ) : null}
              </Pressable>
            ))}
          </>
        ) : null}

        <Text style={styles.sectionLabel}>
          {canEdit ? 'Group backlog — tap a topic to record' : 'Topics discussed'}
        </Text>

        {canEdit ? (
          <>
            <Input
              label="Search backlog"
              value={search}
              onChangeText={setSearch}
              placeholder="Search backlog…"
            />
            <View style={styles.filterRow}>
              {(['all', 'pending', 'discussed'] as ListFilter[]).map((f) => (
                <Pressable
                  key={f}
                  onPress={() => setListFilter(f)}
                  style={[styles.filterChip, listFilter === f && styles.filterChipOn]}>
                  <Text style={[styles.filterText, listFilter === f && styles.filterTextOn]}>
                    {f === 'all' ? 'All' : f === 'pending' ? 'Pending' : 'Discussed'}
                  </Text>
                </Pressable>
              ))}
            </View>
          </>
        ) : null}

        {canEdit
          ? backlogRows.map(({ matter, agendaItem }) => (
              <Pressable
                key={matter.id}
                disabled={busy}
                onPress={() => void handleTopicPress(matter)}
                style={({ pressed }) => [styles.topicRow, pressed && styles.topicRowPressed]}>
                <View style={styles.topicMain}>
                  <Text style={styles.topicTitle}>{matter.title}</Text>
                  {matter.description ? (
                    <Text style={styles.topicSub} numberOfLines={2}>
                      {matter.description}
                    </Text>
                  ) : null}
                  {hasDecision(agendaItem) ? (
                    <Text style={styles.decisionHint}>Group decision recorded</Text>
                  ) : null}
                </View>
                <View style={styles.topicRight}>
                  <StatusBadge
                    compact
                    label={
                      agendaItem
                        ? formatEnumLabel(agendaItem.outcome)
                        : 'Tap to discuss'
                    }
                    variant={
                      agendaItem && isDiscussed(agendaItem.outcome) ? 'success' : 'neutral'
                    }
                  />
                  <Ionicons name="chevron-forward" size={18} color={colors.textLight} />
                </View>
              </Pressable>
            ))
          : publishedAgendaItems.map((item) => (
              <Pressable
                key={item.id}
                onPress={() =>
                  openSheet({
                    kind: 'adhoc',
                    agendaItem: item,
                  })
                }
                style={({ pressed }) => [styles.topicRow, pressed && styles.topicRowPressed]}>
                <View style={styles.topicMain}>
                  <Text style={styles.topicTitle}>
                    {item.agendaNumber}. {item.title}
                  </Text>
                  {item.discussionSummary ? (
                    <Text style={styles.topicSub} numberOfLines={2}>
                      {item.discussionSummary}
                    </Text>
                  ) : null}
                </View>
                <StatusBadge compact label={formatEnumLabel(item.outcome)} />
              </Pressable>
            ))}

        {canEdit && backlogRows.length === 0 ? (
          <Text style={styles.hint}>
            {search ? 'No backlog items match your search.' : 'No open matters in the backlog.'}
          </Text>
        ) : null}

        {canEdit ? (
          <>
            {adHocItems.length > 0 ? (
              <>
                <Text style={styles.sectionLabel}>Pending backlog link</Text>
                {adHocItems.map((item) => (
                  <Pressable
                    key={item.id}
                    onPress={() => handleAdHocPress(item)}
                    style={({ pressed }) => [styles.topicRow, pressed && styles.topicRowPressed]}>
                    <View style={styles.topicMain}>
                      <Text style={styles.topicTitle}>{item.title}</Text>
                      <Text style={styles.topicSub}>Tap to save — will join open matters</Text>
                    </View>
                    <StatusBadge compact label={formatEnumLabel(item.outcome)} />
                  </Pressable>
                ))}
              </>
            ) : null}
            {showAdHocForm ? (
              <View style={styles.adHocForm}>
                <Input
                  label="New point raised in meeting"
                  value={adHocTitle}
                  onChangeText={setAdHocTitle}
                  placeholder="Describe the new topic"
                />
                <Button label="Add & record" onPress={() => void handleAddAdHoc()} disabled={busy} />
                <Button
                  label="Cancel"
                  variant="ghost"
                  onPress={() => {
                    setShowAdHocForm(false);
                    setAdHocTitle('');
                  }}
                />
              </View>
            ) : (
              <Pressable
                onPress={() => setShowAdHocForm(true)}
                style={({ pressed }) => [styles.addRow, pressed && styles.topicRowPressed]}>
                <Ionicons name="add-circle-outline" size={22} color={colors.primary} />
                <Text style={styles.addRowText}>New point raised in meeting</Text>
              </Pressable>
            )}
            <Text style={styles.hint}>
              New points are added to the open matters backlog automatically for tracking in future
              meetings.
            </Text>
          </>
        ) : null}
      </ScrollView>

      <MeetingTopicSheet
        visible={activeTopic !== null}
        title={sheetTitle}
        description={sheetDescription}
        agendaItem={sheetAgenda}
        draft={sheetDraft}
        canEdit={Boolean(canEdit)}
        busy={busy}
        onChangeDraft={setSheetDraft}
        onSave={() => void handleSaveTopic()}
        onClose={closeSheet}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  scroll: { paddingBottom: spacing.xl, gap: spacing.sm },
  headerRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm },
  meetingTitle: { flex: 1, fontSize: 18, fontWeight: '800', color: colors.text },
  meta: { marginTop: 4, fontSize: 12, color: colors.textMuted },
  progressRow: { flexDirection: 'row', justifyContent: 'space-between', marginTop: spacing.sm },
  progressLabel: { fontSize: 11, color: colors.textMuted, fontWeight: '600' },
  progressPct: { fontSize: 11, fontWeight: '800', color: colors.primary },
  progressTrack: {
    height: 6,
    backgroundColor: colors.surfaceMuted,
    borderRadius: 3,
    marginTop: 6,
    marginBottom: spacing.sm,
    overflow: 'hidden',
  },
  progressFill: { height: '100%', backgroundColor: colors.primary, borderRadius: 3 },
  sectionLabel: {
    marginTop: spacing.sm,
    fontSize: 13,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    letterSpacing: 0.4,
  },
  filterRow: { flexDirection: 'row', gap: spacing.xs },
  filterChip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.sm,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  filterChipOn: { borderColor: colors.primaryBorder, backgroundColor: colors.primaryMuted },
  filterText: { fontSize: 12, fontWeight: '600', color: colors.textMuted },
  filterTextOn: { color: colors.primary },
  topicRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
  },
  topicRowPressed: { opacity: 0.9, borderColor: colors.primaryBorder },
  topicMain: { flex: 1, gap: 4 },
  topicTitle: { fontSize: 14, fontWeight: '700', color: colors.text },
  topicSub: { fontSize: 12, color: colors.textMuted, lineHeight: 17 },
  topicRight: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs },
  addRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.sm,
    padding: spacing.md,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderStyle: 'dashed',
    borderColor: colors.primaryBorder,
  },
  addRowText: { fontSize: 14, fontWeight: '600', color: colors.primary },
  adHocForm: { gap: spacing.sm },
  hint: { fontSize: 12, color: colors.textMuted, fontStyle: 'italic' },
  decisionHint: { fontSize: 11, fontWeight: '700', color: colors.success },
  decisionCard: {
    backgroundColor: colors.successMuted,
    borderRadius: radii.lg,
    borderWidth: 1,
    borderColor: colors.successBorder,
    padding: spacing.md,
    gap: spacing.xs,
  },
  decisionText: { fontSize: 14, fontWeight: '700', color: colors.text, lineHeight: 20 },
  resNumber: { fontSize: 11, fontWeight: '800', color: colors.primary },
  budget: { fontSize: 12, fontWeight: '700', color: colors.success },
});
