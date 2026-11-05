"""Read-only App Store readiness report. Never prints credentials or review contacts."""
import json
import sys
from app_store import api_get


def optional(path, params=None):
    try:
        return api_get(path, params)
    except (ValueError, RuntimeError) as error:
        return {'unavailable': str(error)}


def present(attributes, fields):
    return {name: bool(attributes.get(name)) for name in fields}


def inspect(app_id):
    app = api_get('apps/' + app_id)['data']
    result = {'appId': app_id, 'app': {key: app['attributes'].get(key) for key in ('name', 'bundleId', 'primaryLocale', 'contentRightsDeclaration')}}
    builds = api_get('builds', {'filter[app]': app_id, 'sort': '-uploadedDate', 'limit': '10'})
    result['builds'] = [{'id': row['id'], **{key: row['attributes'].get(key) for key in ('version','processingState','uploadedDate','usesNonExemptEncryption','expired')}} for row in builds.get('data', [])]
    result['appInfos'] = []
    for row in api_get('apps/' + app_id + '/appInfos').get('data', []):
        info = {'id': row['id'], 'state': row['attributes'].get('appStoreState')}
        info['categories'] = {key: row.get('relationships', {}).get(key, {}).get('data') for key in ('primaryCategory','secondaryCategory')}
        info['localizations'] = [{'id': loc['id'], 'locale': loc['attributes'].get('locale'), **present(loc['attributes'], ('name','subtitle','privacyPolicyUrl'))} for loc in api_get('appInfos/' + row['id'] + '/appInfoLocalizations').get('data', [])]
        age = optional('appInfos/' + row['id'] + '/ageRatingDeclaration')
        info['ageRating'] = (age.get('data') or {}).get('attributes', age)
        result['appInfos'].append(info)
    result['versions'] = []
    for row in api_get('apps/' + app_id + '/appStoreVersions', {'limit': '20'}).get('data', []):
        version = {'id': row['id'], **{key: row['attributes'].get(key) for key in ('versionString','appStoreState','platform','releaseType','copyright')}}
        version['build'] = optional('appStoreVersions/' + row['id'] + '/relationships/build').get('data')
        review = optional('appStoreVersions/' + row['id'] + '/appStoreReviewDetail')
        attrs = (review.get('data') or {}).get('attributes', {})
        version['reviewDetail'] = {**present(attrs, ('contactFirstName','contactLastName','contactPhone','contactEmail','demoAccountName','demoAccountPassword','notes')), 'demoAccountRequired': attrs.get('demoAccountRequired'), 'available': 'data' in review}
        version['localizations'] = []
        for loc in api_get('appStoreVersions/' + row['id'] + '/appStoreVersionLocalizations').get('data', []):
            localized = {'id': loc['id'], 'locale': loc['attributes'].get('locale'), **present(loc['attributes'], ('description','keywords','supportUrl','marketingUrl','whatsNew'))}
            sets = api_get('appStoreVersionLocalizations/' + loc['id'] + '/appScreenshotSets')
            localized['screenshotSets'] = []
            for images in sets.get('data', []):
                shots = api_get('appScreenshotSets/' + images['id'] + '/appScreenshots')
                localized['screenshotSets'].append({'id': images['id'], 'displayType': images['attributes'].get('screenshotDisplayType'), 'screenshots': [{'id': image['id'], 'deliveryState': image['attributes'].get('assetDeliveryState')} for image in shots.get('data', [])]})
            version['localizations'].append(localized)
        result['versions'].append(version)
    beta = optional('apps/' + app_id + '/betaAppReviewDetail')
    result['betaReviewContact'] = present((beta.get('data') or {}).get('attributes', {}), ('contactFirstName','contactLastName','contactPhone','contactEmail','demoAccountName','demoAccountPassword'))
    result['priceSchedule'] = optional('apps/' + app_id + '/appPriceSchedule')
    result['availability'] = optional('apps/' + app_id + '/appAvailabilityV2')
    result['reviewSubmissions'] = optional('reviewSubmissions', {'filter[app]': app_id, 'limit': '10'})
    # Availability and submission resources contain product/review states, no customer data.
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    inspect(sys.argv[1])
