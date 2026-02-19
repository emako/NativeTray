using NativeTray.Structs;
using NativeTray.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace NativeTray;

/// <summary>
/// Represents a context menu for the tray icon.
/// </summary>
public class TrayMenu : IEnumerable<ITrayMenuItemBase>, IList<ITrayMenuItemBase>
{
    /// <summary>
    /// Gets the parent menu item if this menu is a submenu, otherwise null.
    /// </summary>
    public TrayMenuItem? Parent { get; internal set; }

    private readonly ObservableCollection<ITrayMenuItemBase> _items = [];

    /// <summary>
    /// Gets the list of menu items contained in this menu.
    /// </summary>
    public IList<ITrayMenuItemBase> Items => _items;

    /// <summary>
    /// Gets the number of items in the menu.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Gets a value indicating whether the menu is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets or sets a user-defined tag object.
    /// </summary>
    public object? Tag { get; set; } = null;

    /// <summary>
    /// Gets or sets the menu item at the specified index.
    /// </summary>
    public ITrayMenuItemBase this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    /// <summary>
    /// Occurs before the menu is opened.
    /// </summary>
    public event EventHandler<EventArgs>? Opening;

    /// <summary>
    /// Occurs after the menu is closed.
    /// </summary>
    public event EventHandler<EventArgs>? Closed;

    public IEnumerator<ITrayMenuItemBase> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int IndexOf(ITrayMenuItemBase item) => _items.IndexOf(item);

    public void Insert(int index, ITrayMenuItemBase item) => _items.Insert(index, item);

    public void RemoveAt(int index) => _items.RemoveAt(index);

    public void Add(ITrayMenuItemBase item) => _items.Add(item);

    public void Clear() => _items.Clear();

    public bool Contains(ITrayMenuItemBase item) => _items.Contains(item);

    public void CopyTo(ITrayMenuItemBase[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

    public bool Remove(ITrayMenuItemBase item) => _items.Remove(item);

    public void Open(nint hWnd)
    {
        if (_items.Count == 0) return;

        Opening?.Invoke(this, EventArgs.Empty);

        Dictionary<uint, ITrayMenuItemBase> idToItem = new();
        List<nint> allMenus = new();
        uint currentId = 1000;

        nint hMenu = BuildMenu(_items, idToItem, allMenus, ref currentId);
        if (hMenu == IntPtr.Zero) return;

        _ = User32.GetCursorPos(out POINT pt);

        User32.TrackPopupMenuFlags flag =
            User32.TrackPopupMenuFlags.TPM_RETURNCMD |
            User32.TrackPopupMenuFlags.TPM_VERTICAL |
            User32.TrackPopupMenuFlags.TPM_LEFTALIGN;

        _ = User32.SetForegroundWindow(hWnd);
        uint selected = User32.TrackPopupMenuEx(hMenu, (uint)flag, pt.X, pt.Y, hWnd, IntPtr.Zero);
        _ = User32.PostMessage(hWnd, 0, IntPtr.Zero, IntPtr.Zero);

        if (selected != 0 && idToItem.TryGetValue(selected, out ITrayMenuItemBase? clickedItem))
        {
            clickedItem.Command?.Invoke(clickedItem.CommandParameter);
        }

        // Destroy all menus (main menu and submenus)
        foreach (nint menu in allMenus)
        {
            User32.DestroyMenu(menu);
        }

        Closed?.Invoke(this, EventArgs.Empty);
    }

    private static nint BuildMenu(IList<ITrayMenuItemBase> items, Dictionary<uint, ITrayMenuItemBase> idToItem, List<nint> allMenus, ref uint currentId)
    {
        nint hMenu = User32.CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return IntPtr.Zero;

        allMenus.Add(hMenu);

        foreach (ITrayMenuItemBase item in items)
        {
            if (!item.IsVisible) continue;

            if (item.Header == "-" || item is TraySeparator)
            {
                _ = User32.AppendMenu(hMenu, (uint)User32.MenuFlags.MF_SEPARATOR, 0, string.Empty);
            }
            else
            {
                // Check if this item has a submenu
                TrayMenu? submenu = item.Menu;
                bool hasSubmenu = submenu != null && submenu.Count > 0;

                if (hasSubmenu)
                {
                    // Recursively build the submenu
                    nint hSubMenu = BuildMenu(submenu!.Items, idToItem, allMenus, ref currentId);
                    if (hSubMenu != IntPtr.Zero)
                    {
                        var flags = User32.MenuFlags.MF_STRING | User32.MenuFlags.MF_POPUP;

                        if (!item.IsEnabled)
                            flags |= User32.MenuFlags.MF_DISABLED | User32.MenuFlags.MF_GRAYED;

                        _ = User32.AppendMenu(hMenu, (uint)flags, hSubMenu, item.Header!);
                    }
                }
                else
                {
                    var flags = User32.MenuFlags.MF_STRING;

                    if (!item.IsEnabled)
                        flags |= User32.MenuFlags.MF_DISABLED | User32.MenuFlags.MF_GRAYED;

                    if (item.IsChecked)
                        flags |= User32.MenuFlags.MF_CHECKED;

                    _ = User32.AppendMenu(hMenu, (uint)flags, currentId, item.Header!);

                    if (item.IsBold)
                    {
                        var menuItemInfo = new User32.MENUITEMINFO
                        {
                            cbSize = (uint)Marshal.SizeOf<User32.MENUITEMINFO>(),
                            fMask = (uint)User32.MenuItemMask.MIIM_STATE,
                            fState = (uint)User32.MenuItemState.MFS_DEFAULT
                        };

                        if (item.IsChecked)
                            menuItemInfo.fState |= (uint)User32.MenuItemState.MFS_CHECKED;

                        if (!item.IsEnabled)
                            menuItemInfo.fState |= (uint)User32.MenuItemState.MFS_DISABLED;

                        _ = User32.SetMenuItemInfo(hMenu, currentId, false, ref menuItemInfo);
                    }

                    idToItem[currentId] = item;
                    currentId++;
                }
            }
        }

        return hMenu;
    }
}
