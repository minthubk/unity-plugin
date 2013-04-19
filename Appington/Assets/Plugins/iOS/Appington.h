//
//  Appington.h
//  Appington SDK
//
//  Copyright (c) Appington, Inc. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface Appington : NSObject

// Start the library.  It is safe to call multiple times.
+ (void)start;

// Send a control - see the Events and Campaign Control section of
// the documentation.
+ (void)control:(NSString *)name andValues:(NSDictionary *)values;

// Use this object with the NSNotificationCenter - see the Events and
// Campaign Control section of the documentation.
+ (NSObject *)notificationObject;
@end

