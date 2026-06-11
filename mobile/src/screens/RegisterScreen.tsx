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
import { colors, radii, spacing } from '../theme';

type Props = NativeStackScreenProps<AuthStackParamList, 'Register'>;

export function RegisterScreen({ navigation }: Props) {
  const { register } = useAuth();
  const { showError } = useToast();
  const [phone, setPhone] = useState('');
  const [email, setEmail] = useState('');
  const [name, setName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const onRegister = async () => {
    const trimmedPhone = phone.trim();
    if (!trimmedPhone || !email.trim() || !name.trim() || !password) {
      showError('All fields are required');
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
      await register(trimmedPhone, email.trim().toLowerCase(), name.trim(), password);
    } catch (e) {
      if (e instanceof ApiClientError && e.code === 'PENDING_ACTIVATION') {
        navigation.navigate('ActivateAccount', { phone: trimmedPhone });
        return;
      }
      showError(e instanceof ApiClientError ? e.message : 'Registration failed');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen scroll>
      <View style={styles.hero}>
        <View style={styles.logoMark}>
          <Ionicons name="person-add" size={28} color="#fff" />
        </View>
        <Text style={styles.brand}>Create account</Text>
        <Text style={styles.subtitle}>Register to create and manage your groups</Text>
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
        <Input
          label="Email"
          value={email}
          onChangeText={setEmail}
          keyboardType="email-address"
          autoCapitalize="none"
          autoCorrect={false}
        />
        <Input label="Full name" value={name} onChangeText={setName} autoCapitalize="words" />
        <Input label="Password" value={password} onChangeText={setPassword} secureTextEntry />
        <Input
          label="Confirm password"
          value={confirmPassword}
          onChangeText={setConfirmPassword}
          secureTextEntry
        />
        <Button label="Create account" onPress={onRegister} loading={loading} />
        <Pressable onPress={() => navigation.navigate('ActivateAccount')} style={styles.link}>
          <Text style={styles.linkText}>Invited by your admin? Activate account</Text>
        </Pressable>
        <Pressable onPress={() => navigation.navigate('Login')} style={styles.link}>
          <Text style={styles.linkText}>Already have an account? Sign in</Text>
        </Pressable>
      </View>
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
  brand: { fontSize: 26, fontWeight: '800', color: colors.text, letterSpacing: -0.5 },
  subtitle: { fontSize: 14, color: colors.textMuted, marginTop: 6, textAlign: 'center' },
  formCard: {
    backgroundColor: colors.surface,
    borderRadius: radii.xl,
    padding: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  link: { marginTop: spacing.md, alignItems: 'center' },
  linkText: { color: colors.primary, fontSize: 14, fontWeight: '600' },
});
