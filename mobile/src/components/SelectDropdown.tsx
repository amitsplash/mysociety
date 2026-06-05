import { Ionicons } from '@expo/vector-icons';
import { useState } from 'react';
import {
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { colors, radii, spacing } from '../theme';

export type SelectOption = {
  label: string;
  value: string;
};

interface SelectDropdownProps {
  label: string;
  value: string;
  options: SelectOption[];
  onChange: (value: string) => void;
  disabled?: boolean;
}

export function SelectDropdown({
  label,
  value,
  options,
  onChange,
  disabled,
}: SelectDropdownProps) {
  const [open, setOpen] = useState(false);
  const selected = options.find((option) => option.value === value);

  if (Platform.OS === 'web') {
    return (
      <View style={styles.wrap}>
        <Text style={styles.label}>{label}</Text>
        <select
          value={value}
          disabled={disabled}
          onChange={(event) => onChange(event.target.value)}
          style={{
            width: '100%',
            borderWidth: 1,
            borderStyle: 'solid',
            borderColor: colors.border,
            borderRadius: radii.lg,
            paddingLeft: 14,
            paddingRight: 14,
            paddingTop: 13,
            paddingBottom: 13,
            fontSize: 15,
            backgroundColor: colors.surfaceInset,
            color: colors.text,
            outline: 'none',
          }}>
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </View>
    );
  }

  return (
    <View style={styles.wrap}>
      <Text style={styles.label}>{label}</Text>
      <Pressable
        disabled={disabled}
        onPress={() => setOpen(true)}
        style={({ pressed }) => [
          styles.trigger,
          disabled && styles.triggerDisabled,
          pressed && !disabled && styles.triggerPressed,
        ]}>
        <Text style={styles.triggerText} numberOfLines={1}>
          {selected?.label ?? 'Select'}
        </Text>
        <Ionicons name="chevron-down" size={16} color={colors.textMuted} />
      </Pressable>

      <Modal visible={open} transparent animationType="fade" onRequestClose={() => setOpen(false)}>
        <View style={styles.modalRoot}>
          <Pressable style={styles.backdrop} onPress={() => setOpen(false)} />
          <View style={styles.sheet}>
            <Text style={styles.sheetTitle}>{label}</Text>
            <ScrollView style={styles.optionList} keyboardShouldPersistTaps="handled">
              {options.map((option) => {
                const isActive = option.value === value;
                return (
                  <Pressable
                    key={option.value}
                    onPress={() => {
                      onChange(option.value);
                      setOpen(false);
                    }}
                    style={[styles.optionRow, isActive && styles.optionRowActive]}>
                    <Text style={[styles.optionText, isActive && styles.optionTextActive]}>
                      {option.label}
                    </Text>
                    {isActive ? (
                      <Ionicons name="checkmark" size={18} color={colors.primary} />
                    ) : null}
                  </Pressable>
                );
              })}
            </ScrollView>
          </View>
        </View>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { flex: 1, minWidth: 0 },
  label: {
    fontSize: 10,
    fontWeight: '700',
    color: colors.textMuted,
    marginBottom: 6,
    textTransform: 'uppercase',
    letterSpacing: 0.6,
  },
  trigger: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.lg,
    paddingHorizontal: 14,
    paddingVertical: 13,
    backgroundColor: colors.surfaceInset,
    gap: spacing.sm,
  },
  triggerPressed: { opacity: 0.9 },
  triggerDisabled: { opacity: 0.5 },
  triggerText: { flex: 1, fontSize: 15, color: colors.text, fontWeight: '600' },
  modalRoot: { flex: 1, justifyContent: 'flex-end' },
  backdrop: { ...StyleSheet.absoluteFillObject, backgroundColor: 'rgba(0,0,0,0.45)' },
  sheet: {
    backgroundColor: colors.surface,
    borderTopLeftRadius: radii.xxl,
    borderTopRightRadius: radii.xxl,
    borderTopWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingTop: spacing.md,
    paddingBottom: spacing.lg,
    maxHeight: '55%',
  },
  sheetTitle: {
    fontSize: 14,
    fontWeight: '800',
    color: colors.text,
    marginBottom: spacing.sm,
  },
  optionList: { maxHeight: 320 },
  optionRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  optionRowActive: { backgroundColor: colors.primaryLight },
  optionText: { fontSize: 15, color: colors.text },
  optionTextActive: { color: colors.primaryDark, fontWeight: '700' },
});
