import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ApiClientError, api } from '../api/client';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { AuthStackParamList } from '../navigation/types';
import { colors, spacing } from '../theme';

type Props = NativeStackScreenProps<AuthStackParamList, 'ActivateAccount'>;

export function ActivateAccountScreen({ navigation }: Props) {
  const { completeLogin } = useAuth();
  const { showError, showSuccess } = useToast();
  const [phone, setPhone] = useState('');
  const [inviteCode, setInviteCode] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const onActivate = async () => {
    if (!phone.trim() || !inviteCode.trim() || !password) {
      showError('Phone, invite code, and password are required');
      return;
    }
    if (password.length < 8) {
      showError('Password must be at least 8 characters');
      return;
    }
    if (password !== confirmPassword) {
      showError('Passwords do not match');
      return;
    }

    setLoading(true);
    try {
      const response = await api.activateAccount({
        phone: phone.trim(),
        inviteCode: inviteCode.trim(),
        password,
      });
      await completeLogin(response);
      showSuccess('Account activated!');
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Activation failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen
      title="Activate account"
      subtitle="Use the phone number and invite code from your group admin">
      <Input label="Phone number" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
      <Input
        label="Invite code"
        value={inviteCode}
        onChangeText={setInviteCode}
        autoCapitalize="characters"
      />
      <Text style={styles.hint}>
        Your admin shared these after adding you to the group (e.g. WhatsApp or SMS).
      </Text>
      <Input label="Password" value={password} onChangeText={setPassword} secureTextEntry />
      <Input
        label="Confirm password"
        value={confirmPassword}
        onChangeText={setConfirmPassword}
        secureTextEntry
      />
      <Button label="Activate & sign in" onPress={onActivate} loading={loading} />
      <View style={styles.footer}>
        <Text style={styles.footerText}>Already have a password?</Text>
        <Pressable onPress={() => navigation.navigate('Login')}>
          <Text style={styles.link}>Back to sign in</Text>
        </Pressable>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  hint: {
    color: colors.textMuted,
    fontSize: 12,
    marginTop: -spacing.sm,
    marginBottom: spacing.md,
    lineHeight: 18,
  },
  footer: {
    marginTop: spacing.lg,
    alignItems: 'center',
    gap: spacing.xs,
  },
  footerText: {
    color: colors.textMuted,
    fontSize: 14,
  },
  link: {
    color: colors.primary,
    fontSize: 14,
    fontWeight: '600',
  },
});
