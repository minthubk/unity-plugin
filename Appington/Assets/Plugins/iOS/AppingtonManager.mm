//
//  AppingtonManager.m
//  Unity-iPhone
//
//  Created by Mike Desaro on 4/16/13.
//
//

#import "AppingtonManager.h"
#import "Appington.h"


UIViewController *UnityGetGLViewController();
void UnityPause( bool shouldPause );
void UnitySendMessage( const char * className, const char * methodName, const char * param );


@implementation AppingtonManager

///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark NSObject

+ (AppingtonManager*)sharedManager
{
	static dispatch_once_t pred;
	static AppingtonManager *_sharedInstance = nil;
	
	dispatch_once( &pred, ^{ _sharedInstance = [[self alloc] init]; } );
	return _sharedInstance;
}


- (id)init
{
	if( ( self = [super init] ) )
	{
		[Appington start];
		
		[[NSNotificationCenter defaultCenter] addObserver:self
												 selector:@selector(onAppingtonEvent:)
													 name:nil
												   object:[Appington notificationObject]];
	}
	return self;
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - Class methods

+ (NSString*)objectToJson:(NSObject*)obj
{
	if( NSClassFromString( @"NSJSONSerialization" ) )
	{
		if( [NSJSONSerialization isValidJSONObject:obj] )
		{
			NSError *error = nil;
			NSData *jsonData = [NSJSONSerialization dataWithJSONObject:obj options:0 error:&error];
			
			if( jsonData && !error )
				return [[[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding] autorelease];
			else
				NSLog( @"jsonData was null, error: %@", [error localizedDescription] );
		}
	}
	
	return @"{}";
}


+ (id)objectFromJson:(NSString*)json
{
    Class jsonClass = NSClassFromString( @"NSJSONSerialization" );
    if( jsonClass )
    {
        BOOL respondsToMessage = [jsonClass respondsToSelector:@selector(JSONObjectWithData:options:error:)];
        if( respondsToMessage )
        {
            NSData *jsonData = [json dataUsingEncoding:NSUTF8StringEncoding];
            if( jsonData )
            {
                return [jsonClass JSONObjectWithData:jsonData options:0 error:nil];
            }
            else
            {
                NSLog( @"jsonData was null when converted from the passed in string" );
            }
        } // end if isValid
    } // end if jsonClass
    
    // failure!
    return [NSDictionary dictionary];
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - NSNotification

- (void)onAppingtonEvent:(NSNotification*)notification
{
	NSString *name = [notification name];
	NSDictionary *values = [notification object];
	if( !values )
		values = [NSDictionary dictionary];
	
	// create a hash like so: { name: name, values: values }
	NSDictionary *dict = [NSDictionary dictionaryWithObjectsAndKeys:name, @"name", name, @"values", values, nil];
	
	UnitySendMessage( "AppingtonManager", "onEventOccurred", [AppingtonManager objectToJson:dict].UTF8String );
}


///////////////////////////////////////////////////////////////////////////////////////////////////
#pragma mark - Public

- (void)control:(NSString*)name andValues:(NSDictionary*)values
{
	[Appington control:name andValues:values];
}

@end





extern "C"
{
	#define GetStringParam( _x_ ) ( _x_ != NULL ) ? [NSString stringWithUTF8String:_x_] : [NSString stringWithUTF8String:""]
	
	
	void _appingtonInit()
	{
		[AppingtonManager sharedManager];
	}

	
	void _appingtonControl( const char * name, const char * values )
	{
		NSDictionary *dict = [AppingtonManager objectFromJson:GetStringParam( values )];
		[[AppingtonManager sharedManager] control:GetStringParam( name ) andValues:dict];
	}
}


