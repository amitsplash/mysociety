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

type Props = NativeStackScreenProps<AuthStackParamList, 'ForgotPassword'>;

export function ForgotPasswordScreen({ navigation }: Props) {
  const { completeLogin } = useAuth();
  const { showError, showSuccess } = useToast();
  const [email, setEmail] = useState('');
  const [resetCode, setResetCode] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [codeSent, setCodeSent] = useState(false);
  const [sendLoading, setSendLoading] = useState(false);
  const [resetLoading, setResetLoading] = useState(false);

  const onSendCode = async () => {
    if (!email.trim()) {
      showError('Enter your email address');
      return;
    }

    setSendLoading(true);
    try {
      const response = await api.sendPasswordResetCode({ email: email.trim().toLowerCase() });
      setCodeSent(true);
      showSuccess(response.message);
      if (response.resetCode) {
        setResetCode(response.resetCode);
      }
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to send reset code');
    } finally {
      setSendLoading(false);
    }
  };

  const onReset = async () => {
    if (!email.trim() || !resetCode.trim() || !password) {
      showError('Email, reset code, and password are required');
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

    setResetLoading(true);
    try {
      const response = await api.resetPassword({
        email: email.trim().toLowerCase(),
        resetCode: resetCode.trim(),
        newPassword: password,
      });
      await completeLogin(response);
      showSuccess('Password updated!');
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Password reset failed');
    } finally {
      setResetLoading(false);
    }
  };

  return (
    <Screen
      title="Forgot password"
      subtitle="We'll email you a reset code">
      <Text style={styles.info}>
        Enter the email address on your account. We'll send a 6-digit code you can use to set a
        new password.
      </Text>
      <Input
        label="Email"
        value={email}
        onChangeText={setEmail}
        keyboardType="email-address"
        autoCapitalize="none"
        autoCorrect={false}
      />
      <Button
        label={codeSent ? 'Resend code' : 'Send reset code'}
        onPress={onSendCode}
        loading={sendLoading}
        variant="secondary"
      />
      {codeSent ? (
        <>
          <Input
            label="Reset code"
            value={resetCode}
            onChangeText={setResetCode}
            keyboardType="number-pad"
            maxLength={6}
          />
          <Input label="New password" value={password} onChangeText={setPassword} secureTextEntry />
          <Input
            label="Confirm password"
            value={confirmPassword}
            onChangeText={setConfirmPassword}
            secureTextEntry
          />
          <Button label="Reset & sign in" onPress={onReset} loading={resetLoading} />
        </>
      ) : null}
      <View style={styles.footer}>
        <Pressable onPress={() => navigation.navigate('Login')}>
          <Text style={styles.link}>Back to sign in</Text>
        </Pressable>
      </View>
    </Screen>
  );
}

const styles = StyleSheet.create({
  info: {
    color: colors.textMuted,
    fontSize: 13,
    lineHeight: 20,
    marginBottom: spacing.md,
  },
  footer: {
    marginTop: spacing.lg,
    alignItems: 'center',
  },
  link: {
    color: colors.primary,
    fontSize: 14,
    fontWeight: '600',
  },
});
