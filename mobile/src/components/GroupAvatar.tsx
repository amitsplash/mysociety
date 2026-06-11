import { useState } from 'react';
import { Image, StyleSheet, Text, View, ViewStyle } from 'react-native';
import { colors, radii } from '../theme';
import { getGroupInitials } from '../utils/format';

type Props = {
  name: string;
  logoUrl?: string | null;
  size?: number;
  style?: ViewStyle;
};

export function GroupAvatar({ name, logoUrl, size = 48, style }: Props) {
  const [logoFailed, setLogoFailed] = useState(false);
  const showLogo = Boolean(logoUrl?.trim()) && !logoFailed;

  return (
    <View
      style={[
        styles.base,
        {
          width: size,
          height: size,
          borderRadius: size * 0.28,
        },
        style,
      ]}>
      {showLogo ? (
        <Image
          source={{ uri: logoUrl!.trim() }}
          style={styles.image}
          resizeMode="cover"
          onError={() => setLogoFailed(true)}
        />
      ) : (
        <Text style={[styles.initials, { fontSize: size * 0.34 }]}>{getGroupInitials(name)}</Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  base: {
    backgroundColor: colors.primaryMuted,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  image: {
    width: '100%',
    height: '100%',
  },
  initials: {
    fontWeight: '800',
    color: colors.primary,
    letterSpacing: 0.5,
  },
});
