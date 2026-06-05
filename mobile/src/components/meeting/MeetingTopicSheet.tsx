import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { AgendaItemResponse, MeetingItemOutcome } from '../../api/types';
import { BottomSheet } from '../BottomSheet';
import { Button } from '../Button';
import { Input } from '../Input';
import { StatusBadge } from '../StatusBadge';
import { OutcomeChipPicker } from './OutcomeChipPicker';
import { colors, spacing } from '../../theme';
import { formatEnumLabel } from '../../utils/format';

export type TopicSheetDraft = {
  discussion: string;
  decision: string;
  budget: string;
  outcome: MeetingItemOutcome;
};

type Props = {
  visible: boolean;
  title: string;
  description?: string | null;
  agendaItem: AgendaItemResponse | null;
  draft: TopicSheetDraft;
  canEdit: boolean;
  busy: boolean;
  onChangeDraft: (draft: TopicSheetDraft) => void;
  onSave: () => void;
  onClose: () => void;
};

export function MeetingTopicSheet({
  visible,
  title,
  description,
  agendaItem,
  draft,
  canEdit,
  busy,
  onChangeDraft,
  onSave,
  onClose,
}: Props) {
  const readOnly = !canEdit;
  const finalizedRequiresDecision = draft.outcome === 'Finalized' && !draft.decision.trim();

  return (
    <BottomSheet visible={visible} title={title} onClose={onClose}>
      <ScrollView style={styles.scroll} showsVerticalScrollIndicator={false} keyboardShouldPersistTaps="handled">
        {description ? <Text style={styles.desc}>{description}</Text> : null}
        {agendaItem && readOnly ? (
          <StatusBadge label={formatEnumLabel(agendaItem.outcome)} />
        ) : null}

        {readOnly ? (
          <View style={styles.readOnlyBlock}>
            {draft.discussion ? (
              <>
                <Text style={styles.label}>Discussion</Text>
                <Text style={styles.body}>{draft.discussion}</Text>
              </>
            ) : null}
            {draft.decision ? (
              <View style={styles.decisionBox}>
                <Text style={styles.decisionLabel}>Group decision</Text>
                <Text style={styles.decisionText}>{draft.decision}</Text>
              </View>
            ) : null}
          </View>
        ) : (
          <View style={styles.editBlock}>
            <Input
              label="Discussion"
              value={draft.discussion}
              onChangeText={(discussion) => onChangeDraft({ ...draft, discussion })}
              placeholder="What was discussed in this meeting?"
              multiline
            />

            <Text style={styles.label}>Status</Text>
            <OutcomeChipPicker
              value={draft.outcome}
              onChange={(outcome) => onChangeDraft({ ...draft, outcome })}
              disabled={busy}
            />

            <Text style={styles.helper}>Shown to all members after the meeting is published.</Text>
            <Input
              label="Group decision"
              value={draft.decision}
              onChangeText={(decision) => onChangeDraft({ ...draft, decision })}
              placeholder="Formal decision for residents (required when Finalized)"
              multiline
            />

            <Input
              label="Budget approved (₹)"
              value={draft.budget}
              onChangeText={(budget) => onChangeDraft({ ...draft, budget })}
              placeholder="Optional"
              keyboardType="numeric"
            />

            {finalizedRequiresDecision ? (
              <Text style={styles.validation}>Enter a group decision when status is Finalized.</Text>
            ) : null}

            <Button
              label={busy ? 'Saving…' : 'Save'}
              onPress={onSave}
              disabled={busy || finalizedRequiresDecision}
            />
          </View>
        )}
      </ScrollView>
    </BottomSheet>
  );
}

const styles = StyleSheet.create({
  scroll: { maxHeight: 520 },
  desc: { fontSize: 12, color: colors.textMuted, lineHeight: 18, marginBottom: spacing.sm },
  label: {
    fontSize: 11,
    fontWeight: '700',
    color: colors.textMuted,
    textTransform: 'uppercase',
    marginBottom: spacing.xs,
    marginTop: spacing.sm,
  },
  helper: { fontSize: 11, color: colors.textMuted, marginBottom: spacing.xs },
  body: { fontSize: 14, color: colors.text, lineHeight: 20 },
  readOnlyBlock: { gap: spacing.xs, paddingBottom: spacing.md },
  editBlock: { gap: spacing.xs, paddingBottom: spacing.lg },
  decisionBox: {
    marginTop: spacing.sm,
    padding: spacing.sm,
    backgroundColor: colors.successMuted,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.successBorder,
  },
  decisionLabel: { fontSize: 10, fontWeight: '800', color: colors.success, textTransform: 'uppercase' },
  decisionText: { fontSize: 14, color: colors.text, marginTop: 4, lineHeight: 20 },
  validation: { fontSize: 12, color: colors.danger, fontWeight: '600' },
});
