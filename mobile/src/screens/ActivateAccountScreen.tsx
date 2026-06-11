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

export function ActivateAccountScreen({ navigation, route }: Props) {
  const { completeLogin } = useAuth();
  const { showError, showSuccess } = useToast();
  const [phone, setPhone] = useState(route.params?.phone ?? '');
  const [inviteCode, setInviteCode] = useState(route.params?.inviteCode ?? '');
  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const fromRegisterRedirect = Boolean(route.params?.phone);

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
        email: email.trim() ? email.trim().toLowerCase() : undefined,
        name: name.trim() || undefined,
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
      {fromRegisterRedirect ? (
        <Text style={styles.banner}>
          This phone was added by a group admin. Enter your invite code and set a password to finish
          setting up your account.
        </Text>
      ) : null}
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
      <Input
        label="Email (optional)"
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
        placeholder="For password reset"
      />
      <Input label="Full name (optional)" value={name} onChangeText={setName} autoCapitalize="words" />
      <Input label="Password" value={password} onChangeText={setPassword} secureTextEntry />
      <Input
        label="Confirm password"
        value={confirmPassword}
        onChangeText={setConfirmPassword}
        secureTextEntry
      />
      <Button label="Activate & sign in" onPress={onActivate} loading={loading} />
      <View style={styles.footer}>
        <Text style={styles.footerText}>Starting on your own instead?</Text>
        <Pressable onPress={() => navigation.navigate('Register')}>
          <Text style={styles.link}>Create account</Text>
        </Pressable>
        <Pressable onPress={() => navigation.navigate('Login')} style={styles.footerLink}>
          <Text style={styles.link}>Back to sign in</Text>
        </Pressable>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  banner: {
    color: colors.primary,
    fontSize: 13,
    lineHeight: 19,
    marginBottom: spacing.md,
    padding: spacing.sm,
    backgroundColor: colors.primaryMuted,
    borderRadius: 8,
  },
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
  footerLink: {
    marginTop: spacing.xs,
  },
  link: {
    color: colors.primary,
    fontSize: 14,
    fontWeight: '600',
  },
});
