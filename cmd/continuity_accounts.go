package cmd

import (
	"encoding/json"
	"fmt"
	"os"

	"github.com/TinoA/claude-desktop-switcher/internal/platform"
	"github.com/TinoA/claude-desktop-switcher/internal/profile"
	"github.com/spf13/cobra"
)

type continuityAccount struct {
	Name   string         `json:"name"`
	Health profile.Health `json:"health"`
}

var cmdContinuityAccounts = &cobra.Command{
	Use:   "continuity-accounts",
	Short: "Return account names and local readiness as JSON, without credentials",
	Args:  cobra.NoArgs,
	RunE: func(command *cobra.Command, args []string) error {
		store, err := profile.NewStore()
		if err != nil {
			return err
		}
		profiles, err := store.List()
		if err != nil {
			return err
		}
		appData, err := platform.Current().AppDataPath()
		if err != nil {
			return err
		}
		current := ""
		if profile.HasPersistedAccountStateAt(appData) {
			current, err = store.FindByAccountIdentityAt(appData)
			if err != nil {
				return fmt.Errorf("identify current account: %w", err)
			}
		}
		accounts := make([]continuityAccount, 0, len(profiles))
		for _, meta := range profiles {
			accounts = append(accounts, continuityAccount{Name: meta.Name, Health: store.Inspect(meta.Name).Health})
		}
		return json.NewEncoder(os.Stdout).Encode(struct {
			Current  string              `json:"current"`
			Accounts []continuityAccount `json:"accounts"`
		}{current, accounts})
	},
}

func init() { root.AddCommand(cmdContinuityAccounts) }
