import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Building2 } from 'lucide-react'
import {
  fetchCurrentOrganization,
  updateCurrentOrganization,
  type OrganizationDto,
} from '../../api/organizations'
import { extractApiError } from '../../api/client'
import { useAuthStore } from '../../stores/authStore'
import { toast } from '../../stores/toastStore'
import { Button } from '../../components/ui/Button'
import { Card } from '../../components/ui/Card'
import { Field } from '../../components/ui/Field'

export function OrganizationSettingsPage() {
  const queryClient = useQueryClient()
  const user = useAuthStore((state) => state.user)
  const canManage = user?.role === 'SuperAdmin' || user?.role === 'OrganizationAdmin'

  const orgQuery = useQuery({
    queryKey: ['organization'],
    queryFn: fetchCurrentOrganization,
    retry: false,
  })

  const [name, setName] = useState('')
  const [description, setDescription] = useState('')

  const organization: OrganizationDto | undefined = orgQuery.data
  const isMissing =
    orgQuery.isError && (orgQuery.error as { response?: { status?: number } })?.response?.status === 404

  useEffect(() => {
    if (organization) {
      setName(organization.name)
      setDescription(organization.description ?? '')
    }
  }, [organization])

  const updateMutation = useMutation({
    mutationFn: () =>
      updateCurrentOrganization({ name, description: description || null }),
    onSuccess: (updated) => {
      queryClient.setQueryData(['organization'], updated)
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('Organization updated')
    },
    onError: (error) => toast.error(extractApiError(error, 'Could not update the organization.')),
  })

  if (orgQuery.isLoading) {
    return (
      <Card className="animate-pulse">
        <div className="h-3 w-24 rounded bg-navy-700" />
        <div className="mt-4 h-9 w-full rounded bg-navy-700" />
        <div className="mt-3 h-20 w-full rounded bg-navy-800" />
      </Card>
    )
  }

  if (isMissing) {
    return (
      <div className="mx-auto mt-10 max-w-lg text-center">
        <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl border border-navy-600 bg-navy-850 text-info">
          <Building2 className="h-6 w-6" aria-hidden="true" />
        </div>
        <h1 className="mt-5 text-lg font-semibold text-white">Platform administrator</h1>
        <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-slate-500">
          SuperAdmin accounts oversee all organizations and are not members of one.
          Register an organization or sign in with an organization account to manage its settings.
        </p>
      </div>
    )
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    updateMutation.mutate()
  }

  return (
    <div className="max-w-2xl">
      <h1 className="text-xl font-semibold text-white">Organization settings</h1>
      <p className="mt-1 text-sm text-slate-500">
        Profile information shown across your monitoring workspace.
      </p>

      <Card className="mt-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <Field
            id="org-name"
            label="Organization name"
            value={name}
            onChange={(event) => setName(event.target.value)}
            minLength={3}
            maxLength={200}
            disabled={!canManage}
            required
          />
          <Field
            id="org-slug"
            label="Identifier (slug)"
            value={organization?.slug ?? ''}
            disabled
            hint="The slug is permanent and used in URLs and API scope keys."
          />
          <div>
            <label htmlFor="org-description" className="mb-1.5 block text-xs font-medium text-slate-400">
              Description
            </label>
            <textarea
              id="org-description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              maxLength={1000}
              rows={3}
              disabled={!canManage}
              placeholder="What does this organization monitor?"
              className="w-full resize-none rounded-lg border border-navy-600 bg-navy-900 px-3 py-2 text-sm text-slate-200 placeholder:text-slate-600 focus:border-info/60 focus:outline-none focus:ring-2 focus:ring-info/20 disabled:opacity-60"
            />
          </div>

          {canManage && (
            <div className="flex justify-end pt-1">
              <Button type="submit" loading={updateMutation.isPending}>
                Save changes
              </Button>
            </div>
          )}
        </form>
      </Card>

      {organization && (
        <p className="mt-4 text-xs text-slate-600">
          Created {new Date(organization.createdAtUtc).toLocaleDateString()} ·{' '}
          {user?.role === 'SuperAdmin'
            ? 'Only organization admins can edit this profile.'
            : 'You can edit this profile as an organization admin.'}
        </p>
      )}
    </div>
  )
}
