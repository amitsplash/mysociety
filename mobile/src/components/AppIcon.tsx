import { Ionicons } from '@expo/vector-icons';
import { StyleSheet, View, ViewStyle } from 'react-native';
import { AccentTone, accentTones } from '../theme';

export type IconName = keyof typeof Ionicons.glyphMap;

interface AppIconProps {
  name: IconName;
  size?: number;
  tone?: AccentTone;
  style?: ViewStyle;
}

export function AppIcon({ name, size = 18, tone = 'indigo', style }: AppIconProps) {
  const palette = accentTones[tone];
  return (
    <View style={[styles.wrap, { backgroundColor: palette.bg, borderColor: palette.border }, style]}>
      <Ionicons name={name} size={size} color={palette.text} />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    width: 34,
    height: 34,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
  },
});
