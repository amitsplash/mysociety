import { StyleSheet, Text, TextInput, TextInputProps, View } from 'react-native';
import { colors, radii, spacing } from '../theme';

interface InputProps extends TextInputProps {
  label: string;
  error?: string;
}

export function Input({ label, style, error, ...props }: InputProps) {
  return (
    <View style={styles.wrap}>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        style={[styles.input, error && styles.inputError, style]}
        placeholderTextColor={colors.textLight}
        {...props}
      />
      {error ? <Text style={styles.error}>{error}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { marginBottom: spacing.md },
  label: {
    fontSize: 10,
    fontWeight: '700',
    color: colors.textMuted,
    marginBottom: 6,
    textTransform: 'uppercase',
    letterSpacing: 0.6,
  },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.lg,
    paddingHorizontal: 14,
    paddingVertical: 13,
    fontSize: 15,
    backgroundColor: colors.surfaceInset,
    color: colors.text,
  },
  inputError: { borderColor: colors.danger },
  error: { fontSize: 12, color: colors.danger, marginTop: 4 },
});
