import React from 'react';
import {
  ActivityIndicator,
  FlatList,
  FlatListProps,
  RefreshControl,
  StyleSheet,
  View,
} from 'react-native';
import { colors, spacing } from '../theme';
import { EmptyState } from './EmptyState';

interface ListScreenProps<T> extends Omit<FlatListProps<T>, 'refreshControl'> {
  loading: boolean;
  refreshing: boolean;
  onRefresh: () => void;
  emptyTitle: string;
  emptyMessage?: string;
}

export function ListScreen<T>({
  loading,
  refreshing,
  onRefresh,
  emptyTitle,
  emptyMessage,
  data,
  contentContainerStyle,
  ...rest
}: ListScreenProps<T>) {
  if (loading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  return (
    <FlatList
      data={data}
      refreshControl={
        <RefreshControl
          refreshing={refreshing}
          onRefresh={onRefresh}
          colors={[colors.primary]}
          tintColor={colors.primary}
        />
      }
      contentContainerStyle={[
        styles.list,
        (!data || (data as unknown[]).length === 0) && styles.listEmpty,
        contentContainerStyle,
      ]}
      ListEmptyComponent={<EmptyState title={emptyTitle} message={emptyMessage} />}
      {...rest}
    />
  );
}

const styles = StyleSheet.create({
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.background,
  },
  list: {
    padding: spacing.md,
    paddingBottom: spacing.xl,
    backgroundColor: colors.background,
  },
  listEmpty: { flexGrow: 1 },
});
