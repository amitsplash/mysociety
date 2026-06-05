import { Alert, Platform } from 'react-native';

export async function confirm(
  message: string,
  options?: { title?: string; confirmLabel?: string; cancelLabel?: string },
): Promise<boolean> {
  const title = options?.title ?? 'Confirm';
  const confirmLabel = options?.confirmLabel ?? 'OK';
  const cancelLabel = options?.cancelLabel ?? 'Cancel';

  if (Platform.OS === 'web') {
    return window.confirm(`${title}\n\n${message}`);
  }

  return new Promise((resolve) => {
    Alert.alert(title, message, [
      { text: cancelLabel, style: 'cancel', onPress: () => resolve(false) },
      { text: confirmLabel, style: 'destructive', onPress: () => resolve(true) },
    ]);
  });
}
