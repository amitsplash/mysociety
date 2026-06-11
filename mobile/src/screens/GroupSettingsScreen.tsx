import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useEffect, useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { api, ApiClientError } from '../api/client';
import { Button } from '../components/Button';
import { GroupAvatar } from '../components/GroupAvatar';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, spacing } from '../theme';

type Props = NativeStackScreenProps<MainStackParamList, 'GroupSettings'>;

export function GroupSettingsScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();
  const [tagline, setTagline] = useState('');
  const [logoUrl, setLogoUrl] = useState('');
  const [loading, setLoading] = useState(false);

  const { data: group } = useAsyncData(
    useCallback(() => api.getGroup(groupId, token, memberId), [groupId, token, memberId]),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load group settings' },
  );

  useEffect(() => {
    if (!group) return;
    setTagline(group.tagline ?? '');
    setLogoUrl(group.logoUrl ?? '');
  }, [group]);

  const onSave = async () => {
    if (!group) return;
    setLoading(true);
    try {
      await api.updateGroup(
        groupId,
        {
          name: group.name,
          type: group.type,
          contributionModel: group.contributionModel,
          contributionAmount: group.contributionAmount,
          contributionFrequency: group.contributionFrequency,
          tagline: tagline.trim() || null,
          logoUrl: logoUrl.trim() || null,
        },
        token,
        memberId,
      );
      showSuccess('Group profile updated');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to update group profile');
    } finally {
      setLoading(false);
    }
  };

  if (!group) {
    return (
      <Screen title="Group profile" subtitle="Loading…">
        <Text style={styles.loading}>Loading group settings…</Text>
      </Screen>
    );
  }

  return (
    <Screen title="Group profile" subtitle="Customize how your group appears in the app">
      <GroupAvatar
        name={group.name}
        logoUrl={logoUrl || group.logoUrl}
        size={72}
        style={styles.avatar}
      />
      <Text style={styles.previewName}>{group.name}</Text>
      {tagline.trim() ? <Text style={styles.previewTagline}>{tagline.trim()}</Text> : null}
      <Input
        label="Tagline"
        value={tagline}
        onChangeText={setTagline}
        placeholder="e.g. Building better communities"
        autoCapitalize="sentences"
      />
      <Text style={styles.hint}>Shown on the home screen and group hub.</Text>
      <Input
        label="Logo URL"
        value={logoUrl}
        onChangeText={setLogoUrl}
        placeholder="https://example.com/logo.png"
        autoCapitalize="none"
        autoCorrect={false}
      />
      <Text style={styles.hint}>
        Paste a public image link. If empty or invalid, initials from the group name are shown
        instead.
      </Text>
      <Button label="Save profile" onPress={onSave} loading={loading} />
      <Button
        label="Cancel"
        variant="secondary"
        onPress={() => navigation.goBack()}
        disabled={loading}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  avatar: { alignSelf: 'center', marginBottom: spacing.sm },
  previewName: {
    fontSize: 18,
    fontWeight: '800',
    color: colors.text,
    textAlign: 'center',
    marginBottom: 4,
  },
  previewTagline: {
    fontSize: 13,
    color: colors.textMuted,
    textAlign: 'center',
    marginBottom: spacing.md,
    lineHeight: 18,
  },
  hint: {
    color: colors.textMuted,
    fontSize: 13,
    lineHeight: 18,
    marginBottom: spacing.md,
  },
  loading: { color: colors.textMuted, fontSize: 14 },
});
