import { Pressable, StyleSheet, Text, View, ViewStyle } from 'react-native';
import { colors, radii, spacing } from '../theme';

interface SurfaceCardProps {
  children: React.ReactNode;
  style?: ViewStyle;
  onPress?: () => void;
  variant?: 'default' | 'gradient' | 'dashed';
}

export function SurfaceCard({
  children,
  style,
  onPress,
  variant = 'default',
}: SurfaceCardProps) {
  const cardStyle = [
    styles.card,
    variant === 'gradient' && styles.gradient,
    variant === 'dashed' && styles.dashed,
    style,
  ];

  if (onPress) {
    return (
      <Pressable onPress={onPress} style={({ pressed }) => [cardStyle, pressed && styles.pressed]}>
        {children}
      </Pressable>
    );
  }

  return <View style={cardStyle}>{children}</View>;
}

interface SurfaceCardTitleProps {
  title: string;
  subtitle?: string;
}

export function SurfaceCardTitle({ title, subtitle }: SurfaceCardTitleProps) {
  return (
    <View style={styles.titleWrap}>
      <Text style={styles.title}>{title}</Text>
      {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
    </View>
  );
}

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.xl,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  gradient: {
    backgroundColor: colors.surfaceMuted,
    borderColor: colors.primaryBorder,
  },
  dashed: {
    borderStyle: 'dashed',
    borderColor: colors.primaryBorder,
    backgroundColor: 'rgba(99, 102, 241, 0.05)',
  },
  pressed: { opacity: 0.94 },
  titleWrap: { marginBottom: spacing.sm },
  title: { fontSize: 12, fontWeight: '800', color: colors.text, letterSpacing: 0.4 },
  subtitle: { fontSize: 11, color: colors.textMuted, marginTop: 4, lineHeight: 16 },
});
