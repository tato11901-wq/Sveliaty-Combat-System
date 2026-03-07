# PlayerPrefs Manager for Unity  
**Windows Edition — Editor Only**

**PlayerPrefs Manager** is a Unity Editor extension designed to let you **view**, **edit**, **add**, and **delete** PlayerPrefs directly from within the Unity Editor.  
It’s an essential tool for debugging, testing, and managing key-value data stored via Playerprefs system during development.

---

## 🔧 Features

✔️ View all PlayerPrefs stored by your game  
✔️ Supports all key types: `int`, `float`, and `string`  
✔️ Edit existing PlayerPrefs inline  
✔️ Add new PlayerPrefs with full type and value control  
✔️ Delete individual PlayerPrefs  
✔️ Clear all PlayerPrefs in one click  
✔️ Filter and search keys by name or type  
✔️ Platform-safe implementation (Windows-only)

---

## 🚀 Getting Started

### 1. Open the Tool  
Go to:  
`Tools > SproutStudio > PlayerPrefs Manager`

### 2. View Existing PlayerPrefs  
- All detected keys will be displayed in the left panel.  
- Use the search bar or type filter (`All`, `int`, `float`, `string`) to refine the list.

### 3. Edit a PlayerPref  
- Select a key from the list.  
- The key name, type, and value will appear in the right panel.  
- Click **Edit**, modify the value, and click **Save**.

### 4. Add a New PlayerPref  
- Use the input section at the bottom.  
- Enter a key name, select a data type, and provide a value.  
- Click **Save** to create the new PlayerPref.

### 5. Clear or Cancel  
- **Clear** resets all input fields.  
- **Cancel** reverts any pending edits.

### 6. Delete PlayerPrefs  
- To delete a single key, select it and click **Delete** in the right panel.  
- To remove all PlayerPrefs, click **Delete All Prefs**.

---

## 🗂️ Registry Access

This tool reads and writes directly to the Windows Registry where Unity stores PlayerPrefs:

- **Unity Editor:**  
  `HKEY_CURRENT_USER\Software\Unity\UnityEditor\<CompanyName>\<ProductName>`

> Note: Only keys under your current `CompanyName` and `ProductName` are accessible.

---

## 🖥 Platform Support

✅ **Supported:**  
- Windows (Unity Editor only)

🚧 **Planned:**  
- macOS (plist-based)
- Linux (config folder)

If run on an unsupported platform, the tool will not work as intended and will show errors.

---

## 📬 Support

**Author:** Anilkumar TB  
**Email:** [anilkumartb90712@gmail.com](mailto:anilkumartb90712@gmail.com)

For bug reports, feedback, or feature requests, feel free to get in touch.

---

Thank you for using PlayerPrefs Manager!  
— SproutStudio 🌱
