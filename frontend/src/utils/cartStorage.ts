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
