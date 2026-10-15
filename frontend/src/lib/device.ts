export const isMobileDevice = (strDevice: string | null) => /android|iphone|ipad|mobile/i.test(strDevice ?? '');

/** Rút gọn User-Agent thành tên dễ đọc, vd "Chrome trên Windows" */
export const describeDevice = (strDevice: string | null) => {
  if (!strDevice) return 'Thiết bị không xác định';
  const strBrowser = /edg\//i.test(strDevice)
    ? 'Edge'
    : /chrome\//i.test(strDevice)
      ? 'Chrome'
      : /firefox\//i.test(strDevice)
        ? 'Firefox'
        : /safari\//i.test(strDevice)
          ? 'Safari'
          : 'Trình duyệt';
  const strOs = /windows/i.test(strDevice)
    ? 'Windows'
    : /android/i.test(strDevice)
      ? 'Android'
      : /iphone|ipad|ios/i.test(strDevice)
        ? 'iOS'
        : /mac os/i.test(strDevice)
          ? 'macOS'
          : /linux/i.test(strDevice)
            ? 'Linux'
            : '';
  return strOs ? `${strBrowser} trên ${strOs}` : strBrowser;
};
