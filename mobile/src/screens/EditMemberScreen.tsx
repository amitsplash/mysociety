import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Modal, Pressable, Share, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { IssuePasswordResetResponse, MemberRole } from '../api/types';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing, typography } from '../theme';

type Props = NativeStackScreenProps<MainStackParamList, 'EditMember'>;

const assignableRoles: MemberRole[] = ['Member', 'Admin'];

export function EditMemberScreen({ navigation, route }: Props) {
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();
  const [name, setName] = useState(route.params.name);
  const [phone, setPhone] = useState(route.params.phone);
  const [role, setRole] = useState<MemberRole>(route.params.role);
  const [squareFeet, setSquareFeet] = useState(
    route.params.squareFeet === undefined || route.params.squareFeet === null
      ? ''
      : String(route.params.squareFeet),
  );
  const [loading, setLoading] = useState(false);
  const [resetLoading, setResetLoading] = useState(false);
  const [resetDetails, setResetDetails] = useState<IssuePasswordResetResponse | null>(null);

  const { data: group } = useAsyncData(
    useCallback(() => api.getGroup(groupId, token, memberId), [groupId, token, memberId]),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load group settings' },
  );

  const isPerSquareFeet = group?.contributionModel === 'PerSquareFeet';

  const onIssueReset = async () => {
    setResetLoading(true);
    try {
      const result = await api.issuePasswordReset(route.params.id, token, memberId);
      setResetDetails(result);
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to create reset code');
    } finally {
      setResetLoading(false);
    }
  };

  const shareResetCode = async () => {
    if (!resetDetails) return;
    const identifier = resetDetails.phone
      ? `Phone: ${resetDetails.phone}`
      : `Username: ${resetDetails.username}`;
    const groupLabel = group?.name ?? 'Your group';
    const message = [
      `${groupLabel} — password reset`,
      identifier,
      `Reset code: ${resetDetails.resetCode}`,
      `Expires: ${new Date(resetDetails.expiresAt).toLocaleDateString()}`,
      '',
      'Open the app → Forgot password → enter your email and this code to set a new password.',
    ].join('\n');
    await Share.share({ message });
  };

  const onSubmit = async () => {
    if (!name.trim() || !phone.trim()) {
      showError('Name and phone are required');
      return;
    }

    const parsedSquareFeet = Number(squareFeet);
    if (isPerSquareFeet && (!parsedSquareFeet || parsedSquareFeet <= 0)) {
      showError('Square feet is required for per sq. ft. groups');
      return;
    }

    setLoading(true);
    try {
      await api.updateMember(
        route.params.id,
        {
          name: name.trim(),
          phone: phone.trim(),
          role,
          squareFeet: isPerSquareFeet ? parsedSquareFeet : undefined,
        },
        token,
        memberId,
      );
      showSuccess('Member details updated');
      navigation.goBack();
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to update member');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen title="Edit member" subtitle="Update profile, role, and contribution settings">
      <Input label="Name" value={name} onChangeText={setName} autoCapitalize="words" />
      <Input label="Phone" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />

      <View style={styles.section}>
        <Text style={styles.sectionLabel}>Role</Text>
        <View style={styles.optionWrap}>
          {assignableRoles.map((option) => {
            const isActive = option === role;
            return (
              <Pressable
                key={option}
                onPress={() => setRole(option)}
                style={[styles.option, isActive && styles.optionActive]}>
                <Text style={[styles.optionLabel, isActive && styles.optionLabelActive]}>
                  {option}
                </Text>
              </Pressable>
            );
          })}
        </View>
        <Text style={styles.hint}>Assign Member or Admin role.</Text>
      </View>

      {isPerSquareFeet ? (
        <>
          <Input
            label="Square feet"
            value={squareFeet}
            onChangeText={setSquareFeet}
            keyboardType="decimal-pad"
          />
          <Text style={styles.hint}>
            This group uses per sq. ft. contribution, so square feet is mandatory.
          </Text>
        </>
      ) : null}

      <Button label="Save changes" onPress={onSubmit} loading={loading} />
      <Button
        label="Reset password"
        variant="secondary"
        onPress={onIssueReset}
        loading={resetLoading}
        disabled={loading}
      />
      <Button label="Cancel" variant="secondary" onPress={() => navigation.goBack()} disabled={loading} />
      <Modal visible={resetDetails !== null} transparent animationType="fade">
        <View style={styles.modalBackdrop}>
          <View style={styles.modalCard}>
            <Text style={styles.modalTitle}>Share reset code</Text>
            <Text style={styles.modalBody}>
              Give this code to {resetDetails?.name}. They use Forgot password on the sign-in screen.
            </Text>
            <Text style={styles.codeValue}>{resetDetails?.resetCode}</Text>
            <Text style={styles.modalMeta}>Phone: {resetDetails?.phone}</Text>
            <Button label="Share via WhatsApp / SMS" onPress={shareResetCode} />
            <Pressable onPress={() => setResetDetails(null)} style={styles.doneLink}>
              <Text style={styles.doneText}>Done</Text>
            </Pressable>
          </View>
        </View>
      </Modal>
    </Screen>
  );
}

const styles = StyleSheet.create({
  section: { marginBottom: spacing.md },
  sectionLabel: {
    ...typography.section,
    color: colors.textMuted,
    marginBottom: spacing.sm,
  },
  optionWrap: {
    flexDirection: 'row',
    flexWrap: 'wrap',
  },
  option: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
    borderRadius: radii.md,
    paddingHorizontal: 12,
    paddingVertical: 9,
    marginRight: spacing.sm,
    marginBottom: spacing.sm,
  },
  optionActive: {
    borderColor: colors.borderFocus,
    backgroundColor: colors.primaryLight,
  },
  optionLabel: {
    color: colors.textMuted,
    fontSize: 13,
    fontWeight: '600',
  },
  optionLabelActive: {
    color: colors.primaryDark,
  },
  hint: {
    color: colors.textMuted,
    fontSize: 12,
    lineHeight: 18,
    marginBottom: spacing.md,
  },
  modalBackdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.6)',
    justifyContent: 'center',
    padding: spacing.lg,
  },
  modalCard: {
    backgroundColor: colors.surface,
    borderRadius: 16,
    padding: spacing.lg,
    borderWidth: 1,
    borderColor: colors.border,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '700',
    color: colors.text,
    marginBottom: spacing.sm,
  },
  modalBody: {
    fontSize: 14,
    color: colors.textMuted,
    lineHeight: 20,
    marginBottom: spacing.md,
  },
  modalMeta: {
    fontSize: 14,
    color: colors.text,
    marginBottom: spacing.md,
  },
  codeValue: {
    fontSize: 22,
    fontWeight: '800',
    color: colors.primary,
    letterSpacing: 2,
    marginBottom: spacing.md,
  },
  doneLink: {
    marginTop: spacing.md,
    alignItems: 'center',
  },
  doneText: {
    color: colors.primary,
    fontSize: 15,
    fontWeight: '600',
  },
});
