import { Ionicons } from '@expo/vector-icons';
import { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { api } from '../api/client';
import type { NotificationResponse } from '../api/types';
import { Button } from '../components/Button';
import { ListScreen } from '../components/ListScreen';
import { Screen } from '../components/Screen';
import { useAuth } from '../context/AuthContext';
import { useAsyncData } from '../hooks/useAsyncData';
import { MainStackParamList } from '../navigation/types';
import { colors, radii, spacing } from '../theme';
import { formatDate } from '../utils/format';

type Props = NativeStackScreenProps<MainStackParamList, 'Notifications'>;

export function NotificationsScreen({ navigation }: Props) {
  const { token, memberships, setActiveMembership } = useAuth();

  const { data, loading, refreshing, refresh } = useAsyncData(
    useCallback(() => {
      if (!token) {
        return Promise.resolve([]);
      }
      return api.getNotifications(token);
    }, [token]),
    [token],
    { errorMessage: 'Failed to load notifications', loadOnFocus: true },
  );

  const notifications = data ?? [];
  const unreadCount = notifications.filter((n) => !n.readAt).length;

  const handleMarkAllRead = async () => {
    if (!token || unreadCount === 0) {
      return;
    }
    await api.markAllNotificationsRead(token);
    await refresh();
  };

  const handlePress = async (item: NotificationResponse) => {
    if (!token) {
      return;
    }

    if (!item.readAt) {
      await api.markNotificationRead(item.id, token);
      await refresh();
    }

    if (item.type === 'ContributionsGenerated') {
      const membership = memberships.find((m) => m.groupId === item.groupId);
      if (membership) {
        await setActiveMembership(membership.memberId, membership.groupId);
        navigation.navigate('MainTabs', { screen: 'Payments' });
        return;
      }
    }

    if (item.type === 'MaintenanceDueSoon' || item.type === 'MaintenanceOverdue') {
      const membership = memberships.find((m) => m.groupId === item.groupId);
      if (!membership) {
        return;
      }

      let assetId: string | undefined;
      if (item.dataJson) {
        try {
          const data = JSON.parse(item.dataJson) as { assetId?: string };
          assetId = data.assetId;
        } catch {
          assetId = undefined;
        }
      }

      await setActiveMembership(membership.memberId, membership.groupId);
      if (assetId) {
        navigation.navigate('AssetDetail', { assetId });
      } else {
        navigation.navigate('AssetRegister');
      }
    }
  };

  const ListHeader =
    unreadCount > 0 ? (
      <View style={styles.listHeader}>
        <Button label="Mark all as read" variant="secondary" onPress={() => void handleMarkAllRead()} />
      </View>
    ) : null;

  return (
    <Screen
      title="Notifications"
      subtitle={
        unreadCount > 0
          ? `${unreadCount} unread`
          : `${notifications.length} notification${notifications.length === 1 ? '' : 's'}`
      }
      scroll={false}>
      <ListScreen<NotificationResponse>
        data={notifications}
        loading={loading}
        refreshing={refreshing}
        onRefresh={refresh}
        emptyTitle="No notifications yet"
        emptyMessage="You'll see updates here when contributions are generated or other group activity happens."
        keyExtractor={(item) => item.id}
        ListHeaderComponent={ListHeader}
        renderItem={({ item }) => (
          <Pressable
            onPress={() => void handlePress(item)}
            style={({ pressed }) => [
              styles.card,
              !item.readAt && styles.cardUnread,
              pressed && styles.cardPressed,
            ]}>
            <View style={styles.cardTop}>
              <View style={[styles.iconWrap, !item.readAt && styles.iconWrapUnread]}>
                <Ionicons
                  name={item.type === 'ContributionsGenerated' ? 'wallet-outline' : 'notifications-outline'}
                  size={18}
                  color={!item.readAt ? colors.primary : colors.textMuted}
                />
              </View>
              <View style={styles.cardBody}>
                <Text style={[styles.title, !item.readAt && styles.titleUnread]} numberOfLines={2}>
                  {item.title}
                </Text>
                <Text style={styles.body} numberOfLines={3}>
                  {item.body}
                </Text>
                <Text style={styles.meta}>
                  {item.groupName} · {formatDate(item.createdAt)}
                </Text>
              </View>
              {!item.readAt ? <View style={styles.unreadDot} /> : null}
            </View>
          </Pressable>
        )}
      />
    </Screen>
  );
}

const styles = StyleSheet.create({
  listHeader: {
    marginBottom: spacing.md,
  },
  card: {
    backgroundColor: colors.surface,
    borderRadius: radii.lg,
    padding: spacing.md,
    marginBottom: spacing.sm,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardUnread: {
    borderColor: colors.primaryBorder,
    backgroundColor: colors.surfaceMuted,
  },
  cardPressed: {
    opacity: 0.85,
  },
  cardTop: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.sm,
  },
  iconWrap: {
    width: 36,
    height: 36,
    borderRadius: radii.md,
    backgroundColor: colors.backgroundElevated,
    alignItems: 'center',
    justifyContent: 'center',
  },
  iconWrapUnread: {
    backgroundColor: colors.primaryMuted,
  },
  cardBody: {
    flex: 1,
    gap: 4,
  },
  title: {
    fontSize: 15,
    fontWeight: '600',
    color: colors.text,
  },
  titleUnread: {
    color: colors.text,
  },
  body: {
    fontSize: 13,
    color: colors.textMuted,
    lineHeight: 18,
  },
  meta: {
    fontSize: 12,
    color: colors.textLight,
    marginTop: 2,
  },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: colors.primary,
    marginTop: 6,
  },
});
