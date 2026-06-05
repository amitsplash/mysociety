import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { MeetingItemOutcome } from '../../api/types';
import { colors, radii, spacing } from '../../theme';
import { formatEnumLabel } from '../../utils/format';

const OUTCOMES: MeetingItemOutcome[] = [
  'NotDiscussed',
  'Discussed',
  'Finalized',
  'Postponed',
  'NeedsMoreDiscussion',
];

type Props = {
  value: MeetingItemOutcome;
  onChange: (outcome: MeetingItemOutcome) => void;
  disabled?: boolean;
};

export function OutcomeChipPicker({ value, onChange, disabled }: Props) {
  return (
    <View style={styles.row}>
      {OUTCOMES.map((outcome) => (
        <Pressable
          key={outcome}
          disabled={disabled}
          onPress={() => onChange(outcome)}
          style={[styles.chip, value === outcome && styles.chipSelected]}>
          <Text style={[styles.chipText, value === outcome && styles.chipTextSelected]}>
            {formatEnumLabel(outcome)}
          </Text>
        </Pressable>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  row: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  chip: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.sm,
    paddingHorizontal: 10,
    paddingVertical: 6,
    backgroundColor: colors.surfaceMuted,
  },
  chipSelected: { borderColor: colors.primaryBorder, backgroundColor: colors.primaryMuted },
  chipText: { fontSize: 12, fontWeight: '600', color: colors.textMuted },
  chipTextSelected: { color: colors.primary },
});
