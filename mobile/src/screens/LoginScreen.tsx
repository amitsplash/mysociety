import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { ApiClientError } from '../api/client';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { AuthStackParamList } from '../navigation/types';
import { getApiSetupHint } from '../config/api';
import { colors, radii, spacing } from '../theme';

type Props = NativeStackScreenProps<AuthStackParamList, 'Login'>;

export function LoginScreen({ navigation }: Props) {
  const { login } = useAuth();
  const { showError, showSuccess } = useToast();
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const apiSetupHint = getApiSetupHint();

  const onLogin = async () => {
    const trimmedPhone = phone.trim();
    if (!trimmedPhone || !password) {
      showError('Enter phone number and password');
      return;
    }
    setLoading(true);
    try {
      await login(trimmedPhone, password);
      showSuccess('Welcome back!');
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen scroll>
      <View style={styles.hero}>
        <View style={styles.logoMark}>
          <Ionicons name="shield-checkmark" size={28} color="#fff" />
        </View>
        <Text style={styles.brand}>Society360</Text>
        <Text style={styles.subtitle}>Group contributions, expenses & ledgers</Text>
      </View>
      <View style={styles.formCard}>
        <Input
          label="Phone"
          value={phone}
          onChangeText={setPhone}
          keyboardType="phone-pad"
          autoCapitalize="none"
          autoCorrect={false}
          placeholder="10-digit mobile number"
        />
        <Input label="Password" value={password} onChangeText={setPassword} secureTextEntry />
        <Button label="Sign in" onPress={onLogin} loading={loading} />
        <Pressable onPress={() => navigation.navigate('Register')} style={styles.link}>
          <Text style={styles.linkText}>Create account</Text>
        </Pressable>
        <Pressable onPress={() => navigation.navigate('ActivateAccount')} style={styles.link}>
          <Text style={styles.linkText}>Invited by your admin? Activate account</Text>
        </Pressable>
        <Pressable
          onPress={() => navigation.navigate('ForgotPassword')}
          style={styles.link}>
          <Text style={styles.linkText}>Forgot password?</Text>
        </Pressable>
      </View>
      {apiSetupHint ? (
        <Text style={styles.setupWarning}>{apiSetupHint}</Text>
      ) : null}
    </Screen>
  );
}

const styles = StyleSheet.create({
  hero: { alignItems: 'center', marginBottom: spacing.lg, marginTop: spacing.md },
  logoMark: {
    width: 64,
    height: 64,
    borderRadius: radii.xl,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.primaryBorder,
  },
  brand: { fontSize: 30, fontWeight: '800', color: colors.text, letterSpacing: -0.5 },
  subtitle: { fontSize: 14, color: colors.textMuted, marginTop: 6, textAlign: 'center' },
  formCard: {
    backgroundColor: colors.surface,
    borderRadius: radii.xl,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  setupWarning: {
    fontSize: 12,
    color: colors.danger,
    textAlign: 'center',
    marginTop: spacing.md,
    lineHeight: 18,
  },
  link: {
    marginTop: spacing.md,
    alignItems: 'center',
  },
  linkText: {
    color: colors.primary,
    fontSize: 14,
    fontWeight: '600',
  },
});
