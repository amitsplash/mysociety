import React from 'react';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  StyleSheet,
  Text,
  View,
  ViewStyle,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { colors, spacing, typography } from '../theme';

interface ScreenProps {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  loading?: boolean;
  scroll?: boolean;
  style?: ViewStyle;
}

export function Screen({ title, subtitle, children, loading, scroll = true, style }: ScreenProps) {
  const content = loading ? (
    <View style={styles.loaderWrap}>
      <ActivityIndicator size="large" color={colors.primary} />
    </View>
  ) : (
    children
  );

  return (
    <SafeAreaView style={styles.safe} edges={['top', 'left', 'right']}>
      <KeyboardAvoidingView
        style={[styles.flex, style]}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
        {title ? (
          <View style={styles.header}>
            <Text style={styles.title}>{title}</Text>
            {subtitle ? <Text style={styles.subtitle}>{subtitle}</Text> : null}
          </View>
        ) : null}
        {scroll ? (
          <ScrollView
            contentContainerStyle={styles.scroll}
            keyboardShouldPersistTaps="handled"
            showsVerticalScrollIndicator={false}>
            {content}
          </ScrollView>
        ) : (
          <View style={styles.flex}>{content}</View>
        )}
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.background },
  flex: { flex: 1 },
  header: {
    paddingHorizontal: spacing.md,
    paddingTop: spacing.md,
    paddingBottom: spacing.sm,
    backgroundColor: colors.background,
  },
  scroll: { padding: spacing.md, paddingBottom: spacing.xl },
  title: { ...typography.title, color: colors.text },
  subtitle: { fontSize: 14, color: colors.textMuted, marginTop: 6, lineHeight: 20 },
  loaderWrap: { paddingTop: 80, alignItems: 'center' },
});
