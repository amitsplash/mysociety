import { Pressable, StyleSheet, Text, View, ViewStyle } from 'react-native';
import { colors, radii, spacing } from '../theme';

interface CardProps {
  title?: string;
  subtitle?: string;
  onPress?: () => void;
  children?: React.ReactNode;
  style?: ViewStyle;
  accent?: 'none' | 'indigo';
}

export function Card({ title, subtitle, onPress, children, style, accent = 'none' }: CardProps) {
  const content = (
    <>
      {title ? <Text style={styles.title}>{title}</Text> : null}
      {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
      {children}
    </>
  );

  const cardStyle = [styles.card, accent === 'indigo' && styles.cardIndigo, style];

  if (onPress) {
    return (
      <Pressable
        onPress={onPress}
        style={({ pressed }) => [cardStyle, pressed && styles.pressed]}>
        {content}
      </Pressable>
    );
  }

  return <View style={cardStyle}>{content}</View>;
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    padding: spacing.md,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardIndigo: {
    backgroundColor: colors.primaryMuted,
    borderColor: colors.primaryBorder,
  },
  pressed: { opacity: 0.94, borderColor: colors.primary },
  title: { fontSize: 16, fontWeight: '700', color: colors.text },
  subtitle: { fontSize: 14, color: colors.textMuted, marginTop: 4, lineHeight: 20 },
});
