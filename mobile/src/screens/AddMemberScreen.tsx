import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useState } from 'react';
import { Modal, Pressable, Share, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type { CreateMemberResponse } from '../api/types';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useSession } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, spacing } from '../theme';

type Props = NativeStackScreenProps<MainStackParamList, 'AddMember'>;

function formatExpiry(iso: string | null | undefined): string {
  if (!iso) return '';
  const date = new Date(iso);
  return date.toLocaleDateString(undefined, { dateStyle: 'medium' });
}

export function AddMemberScreen({ navigation }: Props) {
  const { token, memberId, groupId } = useSession();
  const { showSuccess, showError } = useToast();
  const { data: group } = useAsyncData(
    useCallback(() => api.getGroup(groupId, token, memberId), [groupId, token, memberId]),
    [groupId, token, memberId],
    { errorMessage: 'Failed to load group settings' },
  );
  const [name, setName] = useState('');
  const [phone, setPhone] = useState('');
  const [openingBalance, setOpeningBalance] = useState('0');
  const [corpusAmount, setCorpusAmount] = useState('');
  const [corpusPaid, setCorpusPaid] = useState(true);
  const [squareFeet, setSquareFeet] = useState('');
  const [loading, setLoading] = useState(false);
  const [inviteDetails, setInviteDetails] = useState<CreateMemberResponse | null>(null);
  const isPerSquareFeet = group?.contributionModel === 'PerSquareFeet';

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
      const result = await api.createMember(
        {
          groupId,
          name: name.trim(),
          phone: phone.trim(),
          role: 'Member',
          openingBalance: Number(openingBalance) || 0,
          squareFeet: isPerSquareFeet ? parsedSquareFeet : undefined,
          corpusAmount: Number(corpusAmount) || 0,
          corpusPaid: Number(corpusAmount) > 0 ? corpusPaid : false,
        },
        token,
        memberId,
      );
      if (result.requiresActivation && result.inviteCode) {
        setInviteDetails(result);
      } else {
        showSuccess('Member added');
        navigation.goBack();
      }
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to add member');
    } finally {
      setLoading(false);
    }
  };

  const shareInvite = async () => {
    if (!inviteDetails?.inviteCode) return;
    const memberPhone = inviteDetails.member.phone;
    const groupLabel = group?.name ?? 'Your group';
    const message = [
      `${groupLabel} — activate your account`,
      `Phone: ${memberPhone}`,
      `Invite code: ${inviteDetails.inviteCode}`,
      `Expires: ${formatExpiry(inviteDetails.inviteExpiresAt ?? undefined)}`,
      '',
      'Install the app, tap "Activate account" on the sign-in screen, and set your password.',
    ].join('\n');
    await Share.share({ message });
  };

  const closeInviteModal = () => {
    setInviteDetails(null);
    navigation.goBack();
  };

  return (
    <>
      <Screen title="Add member" subtitle="Opening balance: + prepaid credit, − amount already due">
        <Input label="Name" value={name} onChangeText={setName} autoCapitalize="words" />
        <Input label="Phone" value={phone} onChangeText={setPhone} keyboardType="phone-pad" />
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
        <Input
          label="Opening balance (₹)"
          value={openingBalance}
          onChangeText={setOpeningBalance}
          keyboardType="numbers-and-punctuation"
          placeholder="0"
        />
        <Text style={styles.hint}>
          Positive = prepaid credit. Negative = already due, e.g. −1000 if they owe ₹1000 from before.
        </Text>
        <Input
          label="Corpus amount (₹)"
          value={corpusAmount}
          onChangeText={setCorpusAmount}
          keyboardType="decimal-pad"
          placeholder="0 if not applicable"
        />
        {Number(corpusAmount) > 0 ? (
          <>
            <Text style={styles.hint}>
              If corpus was already collected before onboarding, set opening corpus fund on the group
              and mark Yes here — it will not be added to the corpus balance again.
            </Text>
            <View style={styles.corpusPaidRow}>
              <Text style={styles.corpusPaidLabel}>Corpus already paid?</Text>
              <View style={styles.corpusPaidOptions}>
                <Pressable
                  onPress={() => setCorpusPaid(true)}
                  style={[styles.corpusChip, corpusPaid && styles.corpusChipActive]}>
                  <Text style={[styles.corpusChipText, corpusPaid && styles.corpusChipTextActive]}>
                    Yes — received
                  </Text>
                </Pressable>
                <Pressable
                  onPress={() => setCorpusPaid(false)}
                  style={[styles.corpusChip, !corpusPaid && styles.corpusChipActive]}>
                  <Text style={[styles.corpusChipText, !corpusPaid && styles.corpusChipTextActive]}>
                    No — pending
                  </Text>
                </Pressable>
              </View>
            </View>
          </>
        ) : null}
        <Button label="Save member" onPress={onSubmit} loading={loading} />
      </Screen>

      <Modal visible={inviteDetails !== null} transparent animationType="fade">
        <View style={styles.modalBackdrop}>
          <View style={styles.modalCard}>
            <Text style={styles.modalTitle}>Share invite with member</Text>
            <Text style={styles.modalBody}>
              This member must activate their account before they can sign in. Share the phone
              number and invite code below (e.g. WhatsApp or SMS).
            </Text>
            <View style={styles.detailBlock}>
              <Text style={styles.detailLabel}>Phone</Text>
              <Text style={styles.detailValue}>{inviteDetails?.member.phone}</Text>
            </View>
            <View style={styles.detailBlock}>
              <Text style={styles.detailLabel}>Invite code</Text>
              <Text style={styles.codeValue}>{inviteDetails?.inviteCode}</Text>
            </View>
            <View style={styles.detailBlock}>
              <Text style={styles.detailLabel}>Expires</Text>
              <Text style={styles.detailValue}>
                {formatExpiry(inviteDetails?.inviteExpiresAt ?? undefined)}
              </Text>
            </View>
            <Button label="Share invite" onPress={shareInvite} />
            <Pressable onPress={closeInviteModal} style={styles.doneLink}>
              <Text style={styles.doneText}>Done</Text>
            </Pressable>
          </View>
        </View>
      </Modal>
    </>
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
  detailBlock: {
    marginBottom: spacing.md,
  },
  detailLabel: {
    fontSize: 12,
    color: colors.textMuted,
    marginBottom: 4,
  },
  detailValue: {
    fontSize: 16,
    color: colors.text,
    fontWeight: '500',
  },
  codeValue: {
    fontSize: 22,
    fontWeight: '800',
    color: colors.primary,
    letterSpacing: 2,
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
  corpusPaidRow: {
    marginBottom: spacing.md,
  },
  corpusPaidLabel: {
    fontSize: 13,
    fontWeight: '600',
    color: colors.textMuted,
    marginBottom: spacing.xs,
  },
  corpusPaidOptions: {
    flexDirection: 'row',
    gap: spacing.sm,
  },
  corpusChip: {
    flex: 1,
    paddingVertical: spacing.sm,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: 'center',
    backgroundColor: colors.surface,
  },
  corpusChipActive: {
    borderColor: colors.primaryBorder,
    backgroundColor: colors.primaryMuted,
  },
  corpusChipText: {
    fontSize: 12,
    fontWeight: '600',
    color: colors.textMuted,
    textAlign: 'center',
  },
  corpusChipTextActive: {
    color: colors.primary,
  },
});
