import { Ionicons } from '@expo/vector-icons';
import { CompositeScreenProps } from '@react-navigation/native';
import { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { AppIcon } from '../components/AppIcon';
import { Button } from '../components/Button';
import { Screen } from '../components/Screen';
import { SectionHeader } from '../components/SectionHeader';
import { SurfaceCard } from '../components/SurfaceCard';
import { useAuth } from '../context/AuthContext';
import { MainStackParamList, MainTabParamList } from '../navigation/types';
import { colors, spacing, typography } from '../theme';

type Props = CompositeScreenProps<
  BottomTabScreenProps<MainTabParamList, 'Minutes'>,
  NativeStackScreenProps<MainStackParamList>
>;

type HubLink = {
  id: string;
  title: string;
  subtitle: string;
  icon: keyof typeof Ionicons.glyphMap;
  tone: 'indigo' | 'teal' | 'emerald' | 'rose';
  route: 'Meetings' | 'Resolutions' | 'OpenMatters' | 'CommitteeMembers';
  adminOnly?: boolean;
};

const GOVERNANCE_LINKS: HubLink[] = [
  {
    id: 'meetings',
    title: 'Meeting minutes',
    subtitle: 'Agenda, formal minutes & publish',
    icon: 'document-text',
    tone: 'teal',
    route: 'Meetings',
  },
  {
    id: 'resolutions',
    title: 'Group decisions',
    subtitle: 'All published decisions from meetings',
    icon: 'checkmark-done',
    tone: 'emerald',
    route: 'Resolutions',
  },
  {
    id: 'open-matters',
    title: 'Open matters',
    subtitle: 'Cross-meeting discussion backlog',
    icon: 'list',
    tone: 'indigo',
    route: 'OpenMatters',
  },
  {
    id: 'committee-members',
    title: 'Committee members',
    subtitle: 'Assign roles for meeting management',
    icon: 'people-circle',
    tone: 'indigo',
    route: 'CommitteeMembers',
    adminOnly: true,
  },
];

export function GovernanceHubScreen({ navigation }: Props) {
  const { activeMembership, isAdmin } = useAuth();
  const hasActiveGroup = Boolean(activeMembership);

  const visibleLinks = GOVERNANCE_LINKS.filter((l) => !l.adminOnly || isAdmin);

  if (!hasActiveGroup) {
    return (
      <Screen scroll={false}>
        <ScrollView contentContainerStyle={styles.scroll}>
          <Text style={styles.eyebrow}>Minutes & decisions</Text>
          <Text style={styles.name}>Group governance</Text>
          <SurfaceCard variant="gradient">
            <Text style={styles.emptyTitle}>No group selected</Text>
            <Text style={styles.emptyBody}>
              Join or create a group from the Group tab to record meetings, capture decisions, and
              track open matters.
            </Text>
            <Button label="Go to Group" onPress={() => navigation.navigate('Group')} />
          </SurfaceCard>
        </ScrollView>
      </Screen>
    );
  }

  return (
    <Screen scroll={false}>
      <ScrollView contentContainerStyle={styles.scroll} showsVerticalScrollIndicator={false}>
        <Text style={styles.eyebrow}>Minutes & decisions</Text>
        <Text style={styles.name}>{activeMembership?.groupName ?? 'Your group'}</Text>
        <Text style={styles.meta}>
          Meeting minutes, group decisions, and the open-matters backlog
        </Text>

        <SurfaceCard variant="gradient">
          <Text style={styles.cardBody}>
            Committee records meetings here. After publish, all members can read minutes and society
            decisions.
          </Text>
        </SurfaceCard>

        <SectionHeader title="Governance" />
        {visibleLinks.map((link) => (
          <Pressable
            key={link.id}
            onPress={() => navigation.navigate(link.route)}
            style={({ pressed }) => [styles.linkRow, pressed && styles.linkPressed]}>
            <AppIcon name={link.icon} tone={link.tone} />
            <View style={styles.linkText}>
              <Text style={styles.linkTitle}>{link.title}</Text>
              <Text style={styles.linkSub}>{link.subtitle}</Text>
            </View>
            <Ionicons name="chevron-forward" size={18} color={colors.textLight} />
          </Pressable>
        ))}
      </ScrollView>
    </Screen>
  );
}

const styles = StyleSheet.create({
  scroll: { padding: spacing.md, paddingBottom: spacing.xl },
  eyebrow: { ...typography.section, color: colors.textMuted },
  name: { fontSize: 20, fontWeight: '800', color: colors.text, marginTop: 4 },
  meta: { fontSize: 12, color: colors.textMuted, lineHeight: 18, marginTop: 6, marginBottom: spacing.md },
  cardBody: { fontSize: 12, color: colors.textMuted, lineHeight: 18 },
  linkRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.md,
    backgroundColor: colors.surface,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    marginBottom: spacing.sm,
  },
  linkPressed: { opacity: 0.92, borderColor: colors.borderFocus },
  linkText: { flex: 1 },
  linkTitle: { fontSize: 13, fontWeight: '700', color: colors.text },
  linkSub: { fontSize: 10, color: colors.textMuted, marginTop: 2 },
  emptyTitle: { fontSize: 15, fontWeight: '800', color: colors.text, marginBottom: 6 },
  emptyBody: { fontSize: 12, color: colors.textMuted, marginBottom: spacing.md, lineHeight: 18 },
});
