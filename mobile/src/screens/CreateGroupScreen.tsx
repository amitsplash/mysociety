import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api, ApiClientError } from '../api/client';
import type {
  ContributionFrequency,
  ContributionModel,
  GroupType,
} from '../api/types';
import { Button } from '../components/Button';
import { Input } from '../components/Input';
import { Screen } from '../components/Screen';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing, typography } from '../theme';

type Props = NativeStackScreenProps<MainStackParamList, 'CreateGroup'>;

export function CreateGroupScreen({ navigation }: Props) {
  const { token, user, addMembership } = useAuth();
  const { showSuccess, showError } = useToast();
  const [name, setName] = useState('');
  const [tagline, setTagline] = useState('');
  const [groupType, setGroupType] = useState<GroupType>('Rwa');
  const [contributionModel, setContributionModel] = useState<ContributionModel>('Fixed');
  const [contributionFrequency, setContributionFrequency] =
    useState<ContributionFrequency>('Monthly');
  const [amount, setAmount] = useState('2000');
  const [openingMaintenanceBalance, setOpeningMaintenanceBalance] = useState('');
  const [openingCorpusBalance, setOpeningCorpusBalance] = useState('');
  const [creatorOpeningBalance, setCreatorOpeningBalance] = useState('0');
  const [creatorCorpusAmount, setCreatorCorpusAmount] = useState('');
  const [creatorCorpusPaid, setCreatorCorpusPaid] = useState(true);
  const [creatorSquareFeet, setCreatorSquareFeet] = useState('');
  const [loading, setLoading] = useState(false);

  const isPerSquareFeet = contributionModel === 'PerSquareFeet';

  const onSubmit = async () => {
    if (!token) {
      showError('Please sign in first');
      return;
    }
    if (!name.trim()) {
      showError('Enter a group name');
      return;
    }
    const parsedSquareFeet = Number(creatorSquareFeet);
    if (isPerSquareFeet && (!parsedSquareFeet || parsedSquareFeet <= 0)) {
      showError('Your square feet is required for per sq. ft. groups');
      return;
    }

    setLoading(true);
    try {
      const openingBalance = Number(openingMaintenanceBalance) || 0;
      const openingCorpus = Number(openingCorpusBalance) || 0;
      const creatorCorpus = Number(creatorCorpusAmount) || 0;
      const response = await api.createGroup(
        {
          name: name.trim(),
          tagline: tagline.trim() || undefined,
          type: groupType,
          contributionModel,
          contributionAmount: Number(amount) || 0,
          contributionFrequency,
          openingMaintenanceBalance: openingBalance,
          openingCorpusBalance: openingCorpus,
          creatorOpeningBalance: Number(creatorOpeningBalance) || 0,
          creatorSquareFeet: isPerSquareFeet ? parsedSquareFeet : undefined,
          creatorCorpusAmount: creatorCorpus,
          creatorCorpusPaid: creatorCorpus > 0 ? creatorCorpusPaid : false,
        },
        token,
      );
      if (!response?.group?.id || !response?.creatorMember?.id) {
        showError('Unexpected response from server');
        return;
      }

      await addMembership({
        memberId: response.creatorMember.id,
        groupId: response.group.id,
        groupName: response.group.name,
        role: response.creatorMember.role,
      });

      showSuccess(`${response.group.name} created`);
      if (navigation.canGoBack()) {
        navigation.goBack();
      } else {
        navigation.navigate('MainTabs');
      }
    } catch (e) {
      showError(e instanceof ApiClientError ? e.message : 'Failed to create group');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Screen title="Create group" subtitle={`Signed in as ${user?.name ?? 'you'}`}>
      <Input label="Group name" value={name} onChangeText={setName} placeholder="e.g. Sunrise RWA" />
      <Input
        label="Tagline (optional)"
        value={tagline}
        onChangeText={setTagline}
        placeholder="e.g. Building better communities"
        autoCapitalize="sentences"
      />
      <OptionSection
        label="Group type"
        options={[
          { value: 'Rwa', label: 'RWA' },
          { value: 'Friends', label: 'Friends' },
          { value: 'Club', label: 'Club' },
          { value: 'Office', label: 'Office' },
          { value: 'Custom', label: 'Custom' },
        ]}
        selected={groupType}
        onSelect={setGroupType}
      />
      <OptionSection
        label="Contribution model"
        options={[
          { value: 'Fixed', label: 'Fixed amount' },
          { value: 'PerSquareFeet', label: 'Per sq. ft.' },
        ]}
        selected={contributionModel}
        onSelect={setContributionModel}
      />
      <OptionSection
        label="Contribution frequency"
        options={[
          { value: 'Monthly', label: 'Monthly' },
          { value: 'Quarterly', label: 'Quarterly' },
          { value: 'HalfYearly', label: 'Half-yearly' },
          { value: 'Yearly', label: 'Yearly' },
        ]}
        selected={contributionFrequency}
        onSelect={setContributionFrequency}
      />
      <Input
        label={contributionModel === 'Fixed' ? 'Contribution amount (₹)' : 'Rate per sq. ft. (₹)'}
        value={amount}
        onChangeText={setAmount}
        keyboardType="decimal-pad"
      />
      <Input
        label="Opening maintenance fund (₹)"
        value={openingMaintenanceBalance}
        onChangeText={setOpeningMaintenanceBalance}
        keyboardType="decimal-pad"
        placeholder="0"
      />
      <Text style={styles.hint}>Maintenance fund already in the bank when onboarding.</Text>
      <Input
        label="Opening corpus fund (₹)"
        value={openingCorpusBalance}
        onChangeText={setOpeningCorpusBalance}
        keyboardType="decimal-pad"
        placeholder="0"
      />
      <Text style={styles.hint}>Corpus already collected before using the app (optional).</Text>
      <Text style={styles.sectionHeading}>Your membership</Text>
      <Text style={styles.hint}>You will become the group admin for this group.</Text>
      {isPerSquareFeet ? (
        <Input
          label="Your square feet"
          value={creatorSquareFeet}
          onChangeText={setCreatorSquareFeet}
          keyboardType="decimal-pad"
        />
      ) : null}
      <Input
        label="Your opening balance (₹)"
        value={creatorOpeningBalance}
        onChangeText={setCreatorOpeningBalance}
        keyboardType="numbers-and-punctuation"
        placeholder="0"
      />
      <Input
        label="Your corpus amount (₹)"
        value={creatorCorpusAmount}
        onChangeText={setCreatorCorpusAmount}
        keyboardType="decimal-pad"
        placeholder="0"
      />
      {Number(creatorCorpusAmount) > 0 ? (
        <>
          <Text style={styles.hint}>
            If already collected, include it in opening corpus fund above and mark Yes — it will not
            be added to the corpus balance again.
          </Text>
          <OptionSection
          label="Your corpus already paid?"
          options={[
            { value: 'yes', label: 'Yes — received' },
            { value: 'no', label: 'No — pending' },
          ]}
          selected={creatorCorpusPaid ? 'yes' : 'no'}
          onSelect={(v) => setCreatorCorpusPaid(v === 'yes')}
        />
        </>
      ) : null}
      <Button label="Create group" onPress={onSubmit} loading={loading} />
      <Button
        label="Cancel"
        variant="secondary"
        onPress={() => navigation.goBack()}
        disabled={loading}
      />
    </Screen>
  );
}

type OptionSectionProps<T extends string> = {
  label: string;
  options: Array<{ value: T; label: string }>;
  selected: T;
  onSelect: (value: T) => void;
};

function OptionSection<T extends string>({
  label,
  options,
  selected,
  onSelect,
}: OptionSectionProps<T>) {
  return (
    <View style={styles.section}>
      <Text style={styles.sectionLabel}>{label}</Text>
      <View style={styles.optionWrap}>
        {options.map((option) => {
          const isActive = option.value === selected;
          return (
            <Pressable
              key={option.value}
              onPress={() => onSelect(option.value)}
              style={[styles.option, isActive && styles.optionActive]}>
              <Text style={[styles.optionLabel, isActive && styles.optionLabelActive]}>
                {option.label}
              </Text>
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  section: { marginBottom: spacing.md },
  sectionLabel: {
    ...typography.section,
    color: colors.textMuted,
    marginBottom: spacing.sm,
  },
  sectionHeading: {
    fontSize: 15,
    fontWeight: '700',
    color: colors.text,
    marginTop: spacing.sm,
    marginBottom: spacing.xs,
  },
  optionWrap: { flexDirection: 'row', flexWrap: 'wrap' },
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
  optionLabel: { color: colors.textMuted, fontSize: 13, fontWeight: '600' },
  optionLabelActive: { color: colors.primaryDark },
  hint: {
    color: colors.textMuted,
    fontSize: 13,
    lineHeight: 18,
    marginBottom: spacing.md,
  },
});
