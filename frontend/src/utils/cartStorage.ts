const CART_STORAGE_BASE_KEY = "pm_cart_items";

export const getCartStorageKey = (userId?: number | string | null): string => {
  if (userId === undefined || userId === null || userId === "") {
    return CART_STORAGE_BASE_KEY;
  }

  return `${CART_STORAGE_BASE_KEY}:${userId}`;
};

export const getCurrentUserIdFromStorage = (): string | null => {
  return localStorage.getItem("userId");
};

export const getCurrentUserIdentityFromStorage = (): string | null => {
  const userDataRaw = localStorage.getItem("userData");
  if (userDataRaw) {
    try {
      const userData = JSON.parse(userDataRaw) as { email?: string; id?: number | string };
      if (userData?.email) {
        return String(userData.email);
      }

      if (userData?.id !== undefined && userData?.id !== null) {
        return String(userData.id);
      }
    } catch {
      // Fall through to userId when userData parse fails.
    }
  }

  return getCurrentUserIdFromStorage();
};
